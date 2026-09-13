using dailyTimeWorker.Models;
using dailyTimeWorker.Services.Chrome;
using dailyTimeWorker.Services.Notifications;
using dailyTimeWorker.Services.Scheduling;
using dailyTimeWorker.Services.Scraping;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeWorker.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Get() => Ok(new
    {
        service = "dailyTimeWorker",
        status = "ok",
        utc = DateTime.UtcNow
    });
}

[ApiController]
[Route("api/jobs/scrape")]
public class ScrapeJobsController : ControllerBase
{
    private readonly IPortalScrapeService _scrape;
    private readonly IScrapeRunCoordinator _runs;

    public ScrapeJobsController(IPortalScrapeService scrape, IScrapeRunCoordinator runs)
    {
        _scrape = scrape;
        _runs = runs;
    }

    /// <summary>Procesa todos los portales con status queued_playwright.</summary>
    [HttpPost("queued")]
    public async Task<ActionResult<IReadOnlyList<ScrapeResult>>> ProcessQueued(
        CancellationToken cancellationToken)
    {
        var results = await _scrape.ProcessQueuedAsync(cancellationToken);
        return Ok(results);
    }

    [HttpGet("status")]
    public ActionResult<object> Status() => Ok(new
    {
        runningPortalIds = _runs.RunningPortalIds,
        running = _runs.RunningPortalIds.Count > 0
    });

    /// <summary>Detiene el scrape (o la secuencia) en curso sin apagar el worker.</summary>
    [HttpPost("stop")]
    public ActionResult<object> Stop()
    {
        var cancelled = _runs.CancelRunning();
        return Ok(new
        {
            cancelled,
            message = cancelled
                ? "Se pidió detener la captura en curso."
                : "No hay ninguna captura en curso."
        });
    }

    /// <summary>Fuerza scrape de un portal por id (aunque no esté encolado).</summary>
    [HttpPost("{portalId:int}")]
    public async Task<ActionResult<ScrapeResult>> ProcessOne(
        int portalId, CancellationToken cancellationToken)
    {
        if (_runs.RunningPortalIds.Count > 0)
        {
            return Conflict(new
            {
                message = "Ya hay una captura en curso (manual o programada). Espera a que termine o pulsa Detener."
            });
        }

        try
        {
            var result = await _scrape.ProcessPortalAsync(portalId, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return Ok(new ScrapeResult
            {
                PortalId = portalId,
                PortalName = $"portal #{portalId}",
                Status = "cancelled",
                Message = "Captura detenida.",
                Offers = []
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/schedule")]
public class ScheduleController : ControllerBase
{
    private readonly IScrapeScheduleSignal _signal;

    public ScheduleController(IScrapeScheduleSignal signal) => _signal = signal;

    /// <summary>La web lo llama tras guardar el horario para que el worker recalcule la próxima hora.</summary>
    [HttpPost("reload")]
    public ActionResult<object> Reload()
    {
        _signal.Reload();
        return Ok(new { message = "Horario recargado en el worker." });
    }
}

[ApiController]
[Route("api/chrome")]
public class ChromeController : ControllerBase
{
    private readonly IChromeDebugLauncher _launcher;

    public ChromeController(IChromeDebugLauncher launcher) => _launcher = launcher;

    [HttpPost("debug")]
    public async Task<ActionResult<ChromeDebugLaunchResult>> OpenDebug(
        CancellationToken cancellationToken)
    {
        var result = await _launcher.LaunchAsync(cancellationToken);
        if (!result.Started)
            return BadRequest(new { message = result.Message });
        return Ok(result);
    }
}

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) =>
        _notifications = notifications;

    [HttpGet("status")]
    public ActionResult<NotificationResult> Status() => Ok(_notifications.GetStatus());

    [HttpPost("email")]
    public async Task<ActionResult<NotificationResult>> SendEmail(
        [FromBody] SendEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _notifications.SendEmailAsync(request, cancellationToken);
        if (!result.Sent)
            return BadRequest(result);
        return Ok(result);
    }
}
