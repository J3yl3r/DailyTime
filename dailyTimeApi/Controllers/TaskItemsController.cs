using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskItemsController : ControllerBase
    {
        private readonly ITaskItemService _service;

        public TaskItemsController(ITaskItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskItemResponse>>>> GetByDateRange(
            [FromQuery] DateOnly? workDate,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            CancellationToken cancellationToken)
        {
            try
            {
                var start = fromDate ?? workDate;
                var end = toDate ?? workDate;
                if (!start.HasValue || !end.HasValue || start > end)
                    return BadRequest("Debe indicar un rango de fechas válido.");

                var items = await _service.GetRootsByDateRangeAsync(start.Value, end.Value, cancellationToken);
                return Ok(ApiResponse<IReadOnlyList<TaskItemResponse>>.Ok(items));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<TaskItemResponse>>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = await _service.GetByIdAsync(id, cancellationToken);
                return Ok(ApiResponse<TaskItemResponse>.Ok(item));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("{id:int}/children")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TaskItemResponse>>>> GetChildren(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var items = await _service.GetChildrenAsync(id, cancellationToken);
                return Ok(ApiResponse<IReadOnlyList<TaskItemResponse>>.Ok(items));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TaskItemResponse>>> Create(
            [FromBody] CreateTaskItemRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = await _service.CreateAsync(request, cancellationToken);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = item.Id },
                    ApiResponse<TaskItemResponse>.Ok(item, "Tarea creada."));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<TaskItemResponse>>> Update(
            int id,
            [FromBody] UpdateTaskItemRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = await _service.UpdateAsync(id, request, cancellationToken);
                return Ok(ApiResponse<TaskItemResponse>.Ok(item, "Tarea actualizada."));
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
                return Ok(ApiResponse<object>.Ok(null!, "Tarea eliminada."));
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
}