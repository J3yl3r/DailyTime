using dailyTimeWorker.Models;
using dailyTimeWorker.Services.DailyTimeApi;

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
    private readonly ILogger<PortalScrapeService> _logger;

    public PortalScrapeService(
        IDailyTimeApiClient api,
        IPortalScrapeEngine engine,
        ILogger<PortalScrapeService> logger)
    {
        _api = api;
        _engine = engine;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScrapeResult>> ProcessQueuedAsync(
        CancellationToken cancellationToken = default)
    {
        var queued = await _api.GetQueuedPortalsAsync(cancellationToken);
        var results = new List<ScrapeResult>();
        foreach (var portal in queued)
            results.Add(await RunAndPersistAsync(portal, cancellationToken));
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
        JobPortalDto portal, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scrapeando portal {Id} · {Name}", portal.Id, portal.Name);

        await _api.UpdateScrapeRunAsync(portal.Id, new UpdateScrapeRunRequest
        {
            Status = "running",
            Message = "Captura en curso"
        }, cancellationToken);

        ScrapeResult result;
        try
        {
            result = await _engine.ScrapeAsync(portal, cancellationToken);
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

        await _api.UpdateScrapeRunAsync(portal.Id, new UpdateScrapeRunRequest
        {
            Status = result.Status,
            Message = result.Message,
            OfferCount = result.Offers.Count,
            SavedInserted = result.SavedInserted,
            SavedUpdated = result.SavedUpdated
        }, cancellationToken);

        _logger.LogInformation(
            "Portal {Id} → {Status}: {Message}",
            portal.Id, result.Status, result.Message);

        return result;
    }
}
