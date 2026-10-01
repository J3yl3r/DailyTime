using Microsoft.Extensions.Options;

namespace dailyTimeApi.Services.Google;

/// <summary>
/// Corre la sincronización con Google cada cierto intervalo y, además, en cuanto alguien la
/// pide (al guardar un cambio o al tocar los ajustes). No sondea la base de datos: espera en
/// un semáforo con vencimiento.
/// </summary>
public class GoogleCalendarSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GoogleSyncState _state;
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarSyncBackgroundService> _logger;

    public GoogleCalendarSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        GoogleSyncState state,
        IOptions<GoogleCalendarOptions> options,
        ILogger<GoogleCalendarSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (!_options.IsConfigured)
        {
            _logger.LogInformation(
                "Google Calendar sin configurar (faltan Google:ClientId/ClientSecret): no se sincroniza.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Clamp(_options.SyncIntervalMinutes, 1, 1440));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Vuelve al despertar el temporizador o en cuanto alguien pide una pasada.
                await _state.WaitForRunRequestAsync(interval, stoppingToken);
                _state.BeginRun();

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IGoogleCalendarSyncService>();
                var result = await service.SyncAsync(stoppingToken);
                if (result.Pushed > 0 || result.Pulled > 0 || result.Deleted > 0)
                {
                    _logger.LogInformation(
                        "Google Calendar sincronizado: {Pushed} enviados, {Pulled} traídos, {Deleted} borrados.",
                        result.Pushed, result.Pulled, result.Deleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de sincronización con Google Calendar.");
            }
            finally
            {
                _state.EndRun();
            }
        }
    }
}
