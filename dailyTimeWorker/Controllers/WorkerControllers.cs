using dailyTimeWorker.Models;
using dailyTimeWorker.Services.Notifications;
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

    public ScrapeJobsController(IPortalScrapeService scrape) => _scrape = scrape;

    /// <summary>Procesa todos los portales con status queued_playwright.</summary>
    [HttpPost("queued")]
    public async Task<ActionResult<IReadOnlyList<ScrapeResult>>> ProcessQueued(
        CancellationToken cancellationToken)
    {
        var results = await _scrape.ProcessQueuedAsync(cancellationToken);
        return Ok(results);
    }

    /// <summary>Fuerza scrape de un portal por id (aunque no esté encolado).</summary>
    [HttpPost("{portalId:int}")]
    public async Task<ActionResult<ScrapeResult>> ProcessOne(
        int portalId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _scrape.ProcessPortalAsync(portalId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
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
