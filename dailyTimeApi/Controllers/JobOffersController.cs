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
        [FromQuery] int[]? portalIds,
        [FromQuery] string? portalIdsCsv,
        [FromQuery] string? status,
        [FromQuery] string[]? statuses,
        [FromQuery] string? search,
        [FromQuery] string? country,
        [FromQuery] string[]? countries,
        [FromQuery] string? language,
        [FromQuery] string[]? languages,
        [FromQuery] string? workModality,
        [FromQuery] string[]? workModalities,
        [FromQuery] string? contractType,
        [FromQuery] string[]? contractTypes,
        [FromQuery] string? techStack,
        [FromQuery] string[]? techStacks,
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
                PortalIds = ParseIntList(portalId, portalIds, portalIdsCsv),
                Status = status,
                Statuses = ParseStringList(status, statuses),
                Search = search,
                Country = country,
                Countries = ParseStringList(country, countries),
                Language = language,
                Languages = ParseStringList(language, languages),
                WorkModality = workModality,
                WorkModalities = ParseStringList(workModality, workModalities),
                ContractType = contractType,
                ContractTypes = ParseStringList(contractType, contractTypes),
                TechStack = techStack,
                TechStacks = ParseStringList(techStack, techStacks),
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

    [HttpPut("reorder")]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(
        [FromBody] ReorderJobOffersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _service.ReorderAsync(request, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null!, "Orden actualizado."));
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

    private static List<string>? ParseStringList(string? single, string[]? multiple)
    {
        var list = new List<string>();
        if (multiple != null)
        {
            foreach (var m in multiple)
            {
                if (string.IsNullOrWhiteSpace(m)) continue;
                list.AddRange(m.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }
        if (!string.IsNullOrWhiteSpace(single))
        {
            list.AddRange(single.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        var distinct = list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return distinct.Count > 0 ? distinct : null;
    }

    private static List<int>? ParseIntList(int? single, int[]? multiple, string? csv)
    {
        var list = new List<int>();
        if (single.HasValue && single.Value > 0)
            list.Add(single.Value);
        if (multiple != null)
            list.AddRange(multiple.Where(x => x > 0));
        if (!string.IsNullOrWhiteSpace(csv))
        {
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var val) && val > 0)
                    list.Add(val);
            }
        }
        var distinct = list.Distinct().ToList();
        return distinct.Count > 0 ? distinct : null;
    }
}
