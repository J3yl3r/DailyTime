using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/job-portals")]
public class JobPortalsController : ControllerBase
{
    private readonly IJobPortalService _service;
    public JobPortalsController(IJobPortalService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobPortalResponse>>>> GetAll(
        [FromQuery] bool? onlyActive,
        [FromQuery] bool queuedOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _service.GetAllAsync(onlyActive, queuedOnly, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<JobPortalResponse>>.Ok(items));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<JobPortalResponse>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<JobPortalResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobPortalResponse>>> Create(
        [FromBody] CreateJobPortalRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse<JobPortalResponse>.Ok(item, "Portal creado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<JobPortalResponse>>> Update(
        int id, [FromBody] UpdateJobPortalRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<JobPortalResponse>.Ok(item, "Portal actualizado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null!, "Portal eliminado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>
    /// Encola captura del portal. Playwright se conectará en el worker de scraping.
    /// </summary>
    [HttpPost("{id:int}/scrape")]
    public async Task<ActionResult<ApiResponse<JobPortalResponse>>> QueueScrape(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.MarkScrapeQueuedAsync(id, cancellationToken);
            return Ok(ApiResponse<JobPortalResponse>.Ok(
                item,
                "Captura encolada. El worker (dailyTimeWorker) la tomará pronto."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Actualiza el resultado de una corrida (usado por dailyTimeWorker).</summary>
    [HttpPut("{id:int}/run-status")]
    public async Task<ActionResult<ApiResponse<JobPortalResponse>>> UpdateRunStatus(
        int id, [FromBody] UpdateJobPortalRunStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateRunStatusAsync(id, request, cancellationToken);
            return Ok(ApiResponse<JobPortalResponse>.Ok(item, "Estado de captura actualizado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id:int}/scrape-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobPortalScrapeLogResponse>>>> GetScrapeLogs(
        int id, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _service.GetScrapeLogsAsync(id, take, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<JobPortalScrapeLogResponse>>.Ok(items));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    private ActionResult HandleError(Exception ex) => ex switch
    {
        NotFoundException => NotFound(ApiResponse<object>.Fail(ex.Message)),
        ValidationException ve => BadRequest(ApiResponse<object>.Fail(ex.Message, ve.Errors)),
        _ => StatusCode(500, ApiResponse<object>.Fail(
            string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                ? $"Error interno del servidor: {ex.Message}"
                : $"Error interno del servidor: {ex.InnerException.Message}"))
    };
}
