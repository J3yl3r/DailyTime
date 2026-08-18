using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/statuses")]
public class WorkItemStatusesController : ControllerBase
{
    private readonly IWorkItemStatusService _service;

    public WorkItemStatusesController(IWorkItemStatusService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkItemStatusResponse>>>> GetAll(
        [FromQuery] string? itemType,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await _service.GetAllAsync(itemType, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<WorkItemStatusResponse>>.Ok(items));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkItemStatusResponse>>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<WorkItemStatusResponse>.Ok(item));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkItemStatusResponse>>> Create(
        [FromBody] CreateWorkItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = item.Id },
                ApiResponse<WorkItemStatusResponse>.Ok(item, "Estado creado."));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkItemStatusResponse>>> Update(
        int id,
        [FromBody] UpdateWorkItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<WorkItemStatusResponse>.Ok(item, "Estado actualizado."));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null!, "Estado eliminado."));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private ActionResult HandleError(Exception ex) => ex switch
    {
        NotFoundException => NotFound(ApiResponse<object>.Fail(ex.Message)),
        ValidationException ve => BadRequest(ApiResponse<object>.Fail(ex.Message, ve.Errors)),
        _ => StatusCode(500, ApiResponse<object>.Fail("Error interno del servidor."))
    };
}
