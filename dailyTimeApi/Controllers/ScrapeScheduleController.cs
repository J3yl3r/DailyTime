using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

/// <summary>Horario global de captura automática (mismas horas para todos los portales marcados).</summary>
[ApiController]
[Route("api/scrape-schedule")]
public class ScrapeScheduleController : ControllerBase
{
    private readonly IScrapeScheduleService _service;
    public ScrapeScheduleController(IScrapeScheduleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ScrapeScheduleResponse>>> Get(
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetAsync(cancellationToken);
            return Ok(ApiResponse<ScrapeScheduleResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ScrapeScheduleResponse>>> Update(
        [FromBody] UpdateScrapeScheduleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateAsync(request, cancellationToken);
            return Ok(ApiResponse<ScrapeScheduleResponse>.Ok(item, "Horario actualizado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Registra una franja atendida (usado por dailyTimeWorker).</summary>
    [HttpPut("last-slot")]
    public async Task<ActionResult<ApiResponse<ScrapeScheduleResponse>>> MarkSlot(
        [FromBody] MarkScrapeSlotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.MarkSlotAsync(request, cancellationToken);
            return Ok(ApiResponse<ScrapeScheduleResponse>.Ok(item, "Franja registrada."));
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
