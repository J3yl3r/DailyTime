using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/job-offers")]
public class JobOffersController : ControllerBase
{
    private readonly IJobOfferService _service;
    public JobOffersController(IJobOfferService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobOfferResponse>>>> GetAll(
        [FromQuery] int? portalId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? country,
        [FromQuery] string? language,
        [FromQuery] string? workModality,
        [FromQuery] string? contractType,
        [FromQuery] string? techStack,
        [FromQuery] DateTime? capturedFrom,
        [FromQuery] DateTime? capturedTo,
        [FromQuery] DateTime? postedFrom,
        [FromQuery] DateTime? postedTo,
        CancellationToken cancellationToken)
    {
        try
        {
            var filter = new JobOfferFilterRequest
            {
                PortalId = portalId,
                Status = status,
                Search = search,
                Country = country,
                Language = language,
                WorkModality = workModality,
                ContractType = contractType,
                TechStack = techStack,
                CapturedFrom = capturedFrom,
                CapturedTo = capturedTo,
                PostedFrom = postedFrom,
                PostedTo = postedTo
            };
            var items = await _service.GetAllAsync(filter, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<JobOfferResponse>>.Ok(items));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResponse<JobOfferMetaResponse>>> GetMeta(
        CancellationToken cancellationToken)
    {
        try
        {
            var meta = await _service.GetMetaAsync(cancellationToken);
            return Ok(ApiResponse<JobOfferMetaResponse>.Ok(meta));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<JobOfferResponse>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<JobOfferResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Upsert masivo usado por dailyTimeWorker tras un scrape.</summary>
    [HttpPost("upsert")]
    public async Task<ActionResult<ApiResponse<UpsertJobOffersResponse>>> Upsert(
        [FromBody] UpsertJobOffersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpsertBatchAsync(request, cancellationToken);
            return Ok(ApiResponse<UpsertJobOffersResponse>.Ok(
                result,
                $"Ofertas guardadas: {result.Inserted} nuevas, {result.Updated} actualizadas."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<JobOfferResponse>>> UpdateStatus(
        int id, [FromBody] UpdateJobOfferStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateStatusAsync(id, request, cancellationToken);
            return Ok(ApiResponse<JobOfferResponse>.Ok(item, "Estado actualizado."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("bulk/status")]
    public async Task<ActionResult<ApiResponse<BulkJobOfferActionResponse>>> BulkUpdateStatus(
        [FromBody] BulkJobOfferStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.BulkUpdateStatusAsync(request, cancellationToken);
            return Ok(ApiResponse<BulkJobOfferActionResponse>.Ok(
                result, $"{result.Affected} oferta(s) actualizada(s)."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("bulk/delete")]
    public async Task<ActionResult<ApiResponse<BulkJobOfferActionResponse>>> BulkDelete(
        [FromBody] BulkJobOfferDeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.BulkDeleteAsync(request, cancellationToken);
            return Ok(ApiResponse<BulkJobOfferActionResponse>.Ok(
                result, $"{result.Affected} oferta(s) eliminada(s)."));
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
            return Ok(ApiResponse<object>.Ok(null!, "Oferta eliminada."));
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
