using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace dailyTimeApi.Controllers;

/// <summary>Reglas de descarte automático y puntaje de prioridad de ofertas.</summary>
[ApiController]
[Route("api/job-offers/triage")]
public class OfferTriageController : ControllerBase
{
    private readonly IOfferTriageService _service;
    public OfferTriageController(IOfferTriageService service) => _service = service;

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<OfferTriageSettings>>> GetSettings(
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _service.GetSettingsAsync(cancellationToken);
            return Ok(ApiResponse<OfferTriageSettings>.Ok(settings));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Guarda las reglas y recalcula todas las ofertas.</summary>
    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<UpdateOfferTriageResponse>>> UpdateSettings(
        [FromBody] OfferTriageSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateSettingsAsync(settings, cancellationToken);
            return Ok(ApiResponse<UpdateOfferTriageResponse>.Ok(
                result,
                $"Reglas guardadas. Descartadas: {result.Result.NewlyDiscarded} · Restauradas: {result.Result.Restored}."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Vista previa: qué pasaría con las reglas indicadas (o las guardadas), sin guardar nada.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<RescoreJobOffersResponse>>> Preview(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] OfferTriageSettings? settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.PreviewAsync(settings, cancellationToken);
            return Ok(ApiResponse<RescoreJobOffersResponse>.Ok(result));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Recalcula todas las ofertas con las reglas guardadas.</summary>
    [HttpPost("rescore")]
    public async Task<ActionResult<ApiResponse<RescoreJobOffersResponse>>> Rescore(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.RescoreAllAsync(cancellationToken);
            return Ok(ApiResponse<RescoreJobOffersResponse>.Ok(
                result,
                $"Ofertas recalculadas. Descartadas: {result.NewlyDiscarded} · Restauradas: {result.Restored}."));
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
