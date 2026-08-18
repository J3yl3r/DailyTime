using dailyTimeWorker.Configuration;
using Microsoft.Extensions.Options;

namespace dailyTimeWorker.Services.Scraping;

public class PortalScrapeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _options;
    private readonly ILogger<PortalScrapeBackgroundService> _logger;

    public PortalScrapeBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<PortalScrapeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoScrape)
        {
            _logger.LogInformation("Auto-scrape desactivado (Worker:EnableAutoScrape=false).");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds));
        _logger.LogInformation("Worker de scrape activo. Poll cada {Seconds}s.", delay.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scrape = scope.ServiceProvider.GetRequiredService<IPortalScrapeService>();
                var results = await scrape.ProcessQueuedAsync(stoppingToken);
                if (results.Count > 0)
                {
                    _logger.LogInformation("Procesados {Count} portales encolados.", results.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de scrape.");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
