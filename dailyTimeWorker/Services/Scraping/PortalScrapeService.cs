using dailyTimeWorker.Configuration;
using dailyTimeWorker.Models;
using dailyTimeWorker.Services.Chrome;
using dailyTimeWorker.Services.DailyTimeApi;
using Microsoft.Extensions.Options;

namespace dailyTimeWorker.Services.Scraping;

public interface IPortalScrapeService
{
    Task<IReadOnlyList<ScrapeResult>> ProcessQueuedAsync(CancellationToken cancellationToken = default);
    Task<ScrapeResult> ProcessPortalAsync(int portalId, CancellationToken cancellationToken = default);
}

public class PortalScrapeService : IPortalScrapeService
{
    private readonly IDailyTimeApiClient _api;
    private readonly IPortalScrapeEngine _engine;
    private readonly IScrapeRunCoordinator _runs;
    private readonly IChromeDebugLauncher _chrome;
    private readonly WorkerOptions _options;
    private readonly ILogger<PortalScrapeService> _logger;

    public PortalScrapeService(
        IDailyTimeApiClient api,
        IPortalScrapeEngine engine,
        IScrapeRunCoordinator runs,
        IChromeDebugLauncher chrome,
        IOptions<WorkerOptions> options,
        ILogger<PortalScrapeService> logger)
    {
        _api = api;
        _engine = engine;
        _runs = runs;
        _chrome = chrome;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScrapeResult>> ProcessQueuedAsync(
        CancellationToken cancellationToken = default)
    {
        var queued = await _api.GetQueuedPortalsAsync(cancellationToken);
        var results = new List<ScrapeResult>();
        foreach (var portal in queued)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await RunAndPersistAsync(portal, cancellationToken));
        }
        return results;
    }

    public async Task<ScrapeResult> ProcessPortalAsync(
        int portalId, CancellationToken cancellationToken = default)
    {
        var portal = await _api.GetPortalAsync(portalId, cancellationToken)
            ?? throw new InvalidOperationException($"Portal {portalId} no encontrado en DailyTime API.");
        return await RunAndPersistAsync(portal, cancellationToken);
    }

    private async Task<ScrapeResult> RunAndPersistAsync(
        JobPortalDto portal, CancellationToken requestToken)
    {
        var cancellationToken = _runs.Register(portal.Id, requestToken);
        _logger.LogInformation("Scrapeando portal {Id} · {Name}", portal.Id, portal.Name);

        try
        {
            try
            {
                await EnsureChromeDebugAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var cancelledChrome = Cancelled(portal);
                await PersistRunStatusAsync(portal.Id, cancelledChrome);
                return cancelledChrome;
            }

            try
            {
                await _api.UpdateScrapeRunAsync(portal.Id, new UpdateScrapeRunRequest
                {
                    Status = "running",
                    Message = "Captura en curso"
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var cancelledEarly = Cancelled(portal);
                await PersistRunStatusAsync(portal.Id, cancelledEarly);
                return cancelledEarly;
            }

            ScrapeResult result;
            try
            {
                result = await _engine.ScrapeAsync(portal, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                result = Cancelled(portal);
                await PersistRunStatusAsync(portal.Id, result);
                return result;
            }
            catch (Exception ex)
            {
                result = new ScrapeResult
                {
                    PortalId = portal.Id,
                    PortalName = portal.Name,
                    Status = "error",
                    Message = ex.Message,
                    Offers = []
                };
            }

            if (result.Offers.Count > 0)
            {
                try
                {
                    var upsert = await _api.UpsertOffersAsync(new UpsertJobOffersRequest
                    {
                        JobPortalId = portal.Id,
                        Offers = result.Offers.Select(o => new UpsertJobOfferItem
                        {
                            Title = o.Title,
                            Company = o.Company,
                            Location = o.Location,
                            Url = o.Url,
                            ExternalKey = o.ExternalKey,
                            DescriptionSnippet = o.DescriptionSnippet
                                ?? (o.Description is { Length: > 0 }
                                    ? (o.Description.Length <= 2000 ? o.Description : o.Description[..2000])
                                    : null),
                            Description = o.Description ?? o.DescriptionSnippet,
                            Country = o.Country,
                            Language = o.Language,
                            PostedAt = o.PostedAt,
                            WorkModality = o.WorkModality,
                            ContractType = o.ContractType,
                            TechStack = o.TechStack
                        }).ToList()
                    }, cancellationToken);

                    result = result with
                    {
                        Message =
                            $"{result.Message} Guardadas en BD: {upsert.Inserted} nuevas, {upsert.Updated} actualizadas.",
                        SavedInserted = upsert.Inserted,
                        SavedUpdated = upsert.Updated
                    };
                }
                catch (OperationCanceledException)
                {
                    result = Cancelled(portal);
                    await PersistRunStatusAsync(portal.Id, result);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudieron guardar ofertas del portal {Id}", portal.Id);
                    result = result with
                    {
                        Status = "error",
                        Message = $"Scrape OK pero falló al guardar en BD: {ex.Message}"
                    };
                }
            }

            await PersistRunStatusAsync(portal.Id, result);
            _logger.LogInformation(
                "Portal {Id} → {Status}: {Message}",
                portal.Id, result.Status, result.Message);
            return result;
        }
        finally
        {
            _runs.Unregister(portal.Id);
            if (_options.UseRemoteDebuggingBrowser && _runs.RunningPortalIds.Count == 0)
                _chrome.ScheduleCloseWhenIdle();
        }
    }

    private static ScrapeResult Cancelled(JobPortalDto portal) => new()
    {
        PortalId = portal.Id,
        PortalName = portal.Name,
        Status = "cancelled",
        Message = "Captura detenida.",
        Offers = []
    };

    private async Task PersistRunStatusAsync(int portalId, ScrapeResult result)
    {
        try
        {
            await _api.UpdateScrapeRunAsync(portalId, new UpdateScrapeRunRequest
            {
                Status = result.Status,
                Message = result.Message,
                OfferCount = result.Offers.Count,
                SavedInserted = result.SavedInserted,
                SavedUpdated = result.SavedUpdated
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo persistir el estado final del portal {Id}", portalId);
        }
    }

    private async Task EnsureChromeDebugAsync(CancellationToken cancellationToken)
    {
        if (!_options.UseRemoteDebuggingBrowser)
            return;

        var result = await _chrome.LaunchAsync(cancellationToken).ConfigureAwait(false);
        if (result.Started)
        {
            _logger.LogInformation("{Message}", result.Message);
            return;
        }

        _logger.LogWarning("Chrome debug no se abrió antes del scrape: {Message}", result.Message);
    }
}
