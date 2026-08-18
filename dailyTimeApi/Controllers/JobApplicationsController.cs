using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/job-applications")]
public class JobApplicationsController : ControllerBase
{
    private readonly IJobApplicationService _service;
    public JobApplicationsController(IJobApplicationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobApplicationResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await _service.GetAllAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<JobApplicationResponse>>.Ok(items));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<JobApplicationResponse>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<JobApplicationResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobApplicationResponse>>> Create(
        [FromBody] CreateJobApplicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse<JobApplicationResponse>.Ok(item, "Postulación creada."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<JobApplicationResponse>>> Update(
        int id, [FromBody] UpdateJobApplicationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<JobApplicationResponse>.Ok(item, "Postulación actualizada."));
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
            return Ok(ApiResponse<object>.Ok(null!, "Postulación eliminada."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("bulk/delete")]
    public async Task<ActionResult<ApiResponse<BulkJobApplicationActionResponse>>> BulkDelete(
        [FromBody] BulkJobApplicationDeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.BulkDeleteAsync(request, cancellationToken);
            return Ok(ApiResponse<BulkJobApplicationActionResponse>.Ok(
                result, $"{result.Affected} postulación(es) eliminada(s)."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    private ActionResult HandleError(Exception ex) => ex switch
    {
        NotFoundException => NotFound(ApiResponse<object>.Fail(ex.Message)),
        ValidationException ve => BadRequest(ApiResponse<object>.Fail(ex.Message, ve.Errors)),
        _ => StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."))
    };
}
