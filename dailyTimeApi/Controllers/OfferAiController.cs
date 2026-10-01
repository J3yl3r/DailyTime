using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace dailyTimeApi.Controllers;

/// <summary>Análisis de ofertas con IA (Gemini).</summary>
[ApiController]
[Route("api/job-offers/ai")]
public class OfferAiController : ControllerBase
{
    private readonly IOfferAiService _service;
    public OfferAiController(IOfferAiService service) => _service = service;

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<OfferAiStatusResponse>>> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ApiResponse<OfferAiStatusResponse>.Ok(await _service.GetStatusAsync(cancellationToken)));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Pide analizar ya las ofertas activas pendientes (corre en segundo plano).</summary>
    [HttpPost("analyze-pending")]
    public async Task<ActionResult<ApiResponse<OfferAiStatusResponse>>> AnalyzePending(CancellationToken cancellationToken)
    {
        try
        {
            _service.RequestProcessing();
            return Ok(ApiResponse<OfferAiStatusResponse>.Ok(
                await _service.GetStatusAsync(cancellationToken), "Análisis de pendientes solicitado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>
    /// Pone en cola las ofertas activas capturadas ese día (mismo criterio que el filtro de fecha de captura)
    /// sin análisis o cuyo análisis falló; corre en segundo plano.
    /// </summary>
    [HttpPost("analyze-day")]
    public async Task<ActionResult<ApiResponse<OfferAiQueueResponse>>> AnalyzeDay(
        [FromQuery, BindRequired] DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            var queued = await _service.QueueCapturedOnAsync(date, cancellationToken);
            return Ok(ApiResponse<OfferAiQueueResponse>.Ok(new OfferAiQueueResponse
            {
                Queued = queued,
                Status = await _service.GetStatusAsync(cancellationToken)
            }));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Analiza o reanaliza una oferta ahora (espera la respuesta de Gemini).</summary>
    [HttpPost("{id:int}/analyze")]
    public async Task<ActionResult<ApiResponse<JobOfferResponse>>> Analyze(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.AnalyzeOneAsync(id, cancellationToken);
            return Ok(ApiResponse<JobOfferResponse>.Ok(item, "Oferta analizada."));
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
