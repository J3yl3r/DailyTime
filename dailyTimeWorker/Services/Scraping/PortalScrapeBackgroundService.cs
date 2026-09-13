using dailyTimeWorker.Configuration;
using dailyTimeWorker.Models;
using dailyTimeWorker.Services.DailyTimeApi;
using dailyTimeWorker.Services.Scheduling;
using Microsoft.Extensions.Options;

namespace dailyTimeWorker.Services.Scraping;

/// <summary>
/// Ejecuta la captura automática según el horario global de la API. No consulta periódicamente:
/// calcula la próxima hora y espera con un temporizador de Windows (que además despierta el equipo).
/// Si la web guarda un horario nuevo, <see cref="IScrapeScheduleSignal"/> interrumpe la espera.
/// </summary>
public class PortalScrapeBackgroundService : BackgroundService
{
    /// <summary>Despierta el equipo un poco antes para que la red esté lista a la hora exacta.</summary>
    private static readonly TimeSpan WakeLead = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScrapeScheduleSignal _signal;
    private readonly IScrapeRunCoordinator _runs;
    private readonly WorkerOptions _options;
    private readonly ILogger<PortalScrapeBackgroundService> _logger;

    public PortalScrapeBackgroundService(
        IServiceScopeFactory scopeFactory,
        IScrapeScheduleSignal signal,
        IScrapeRunCoordinator runs,
        IOptions<WorkerOptions> options,
        ILogger<PortalScrapeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = signal;
        _runs = runs;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoScrape)
        {
            _logger.LogInformation("Captura programada desactivada (Worker:EnableAutoScrape=false).");
            return;
        }

        await Task.Yield();
        var firstIteration = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            var reloadToken = _signal.Token;
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, reloadToken);

            try
            {
                ScrapeScheduleDto schedule;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var api = scope.ServiceProvider.GetRequiredService<IDailyTimeApiClient>();
                    schedule = await api.GetScheduleAsync(stoppingToken);

                    if (firstIteration)
                    {
                        firstIteration = false;
                        // Al arrancar no puede haber una ejecución real en curso: quedó colgada por un cierre.
                        if (schedule.LastSlotStatus == "running" && schedule.LastSlotAt is { } stale)
                        {
                            await api.MarkScheduleSlotAsync(new MarkScrapeSlotRequest
                            {
                                SlotAt = stale,
                                Status = "failed",
                                Message = "El worker se detuvo durante la ejecución.",
                                FinishedAt = stale
                            }, stoppingToken);
                            continue;
                        }
                    }

                    if (schedule.MissedSlotAt is { } missed)
                    {
                        _logger.LogInformation("Captura de las {Slot:g} omitida: el equipo estaba apagado, suspendido u ocupado.",
                            missed.ToLocalTime());
                        await api.MarkScheduleSlotAsync(new MarkScrapeSlotRequest
                        {
                            SlotAt = missed,
                            Status = "skipped",
                            Message = "A esa hora el equipo estaba apagado, suspendido o con otra captura en curso."
                        }, stoppingToken);
                        continue;
                    }
                }

                if (!schedule.Enabled || schedule.NextSlotAt is null)
                {
                    _logger.LogInformation("Sin horario de captura activo. Esperando cambios desde la web.");
                    await Task.Delay(Timeout.Infinite, waitCts.Token);
                    continue;
                }

                var slot = schedule.NextSlotAt.Value;
                if (slot > DateTime.UtcNow)
                {
                    _logger.LogInformation("Próxima captura programada: {Slot:g}.", slot.ToLocalTime());
                    var wakeAt = slot - WakeLead;
                    if (wakeAt > DateTime.UtcNow)
                        await WindowsPower.WaitUntilAsync(wakeAt, wakeSystem: true, _logger, waitCts.Token);
                    await WindowsPower.WaitUntilAsync(slot, wakeSystem: true, _logger, waitCts.Token);
                    // Se vuelve a consultar: si el equipo despertó tarde, la API la reporta como perdida.
                    continue;
                }

                await RunSlotAsync(slot, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException) when (reloadToken.IsCancellationRequested)
            {
                _logger.LogInformation("Horario de captura actualizado; recalculando la próxima hora.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo leer o atender el horario de captura. Reintento en {Seconds}s.",
                    RetryDelay.TotalSeconds);
                try
                {
                    await Task.Delay(RetryDelay, waitCts.Token);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    /* recarga: reintenta ya */
                }
            }
        }
    }

    private async Task RunSlotAsync(DateTime slot, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IDailyTimeApiClient>();
        var scrape = scope.ServiceProvider.GetRequiredService<IPortalScrapeService>();

        if (_runs.RunningPortalIds.Count > 0)
        {
            await MarkAsync(api, slot, "skipped", "Había una captura manual en curso.", null);
            return;
        }

        var portals = await api.GetAutoPortalsAsync(stoppingToken);
        if (portals.Count == 0)
        {
            await MarkAsync(api, slot, "skipped", "No hay portales activos marcados como Automática.", null);
            return;
        }

        _logger.LogInformation("Captura programada de las {Slot:g}: {Count} portal(es).", slot.ToLocalTime(), portals.Count);
        await MarkAsync(api, slot, "running", $"Ejecutando {portals.Count} portal(es) en secuencia.", null);

        using var keepAwake = WindowsPower.KeepSystemAwake("DailyTime: captura programada de portales", _logger);
        int ok = 0, failed = 0, inserted = 0, updated = 0;
        var cancelled = false;

        foreach (var portal in portals)
        {
            ScrapeResult result;
            try
            {
                result = await scrape.ProcessPortalAsync(portal.Id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw; // al reiniciar, la franja en curso se marca como fallida
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Error en la captura programada del portal {Id} · {Name}", portal.Id, portal.Name);
                continue;
            }

            if (result.Status == "cancelled")
            {
                cancelled = true;
                break;
            }

            if (result.Status is "error" or "blocked")
            {
                failed++;
            }
            else
            {
                ok++;
                inserted += result.SavedInserted;
                updated += result.SavedUpdated;
            }
        }

        var status = cancelled ? "cancelled" : ok == 0 && failed > 0 ? "failed" : "completed";
        var message = $"OK: {ok} · Errores: {failed} · Nuevas: {inserted} · Actualizadas: {updated}";
        if (cancelled)
            message += " · Detenida manualmente.";

        _logger.LogInformation("Captura programada de las {Slot:g} → {Status}. {Message}", slot.ToLocalTime(), status, message);
        await MarkAsync(api, slot, status, message, DateTime.UtcNow);
    }

    private async Task MarkAsync(
        IDailyTimeApiClient api, DateTime slot, string status, string message, DateTime? finishedAt)
    {
        try
        {
            await api.MarkScheduleSlotAsync(new MarkScrapeSlotRequest
            {
                SlotAt = slot,
                Status = status,
                Message = message,
                FinishedAt = finishedAt
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar la franja {Slot:g} ({Status}).", slot.ToLocalTime(), status);
        }
    }
}
