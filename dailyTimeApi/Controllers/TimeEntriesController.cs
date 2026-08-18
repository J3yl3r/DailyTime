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
    public class TimeEntriesController : ControllerBase
    {
        private readonly ITimeEntryService _service;

        public TimeEntriesController(ITimeEntryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TimeEntryResponse>>>> GetByDateRange(
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

                var items = await _service.GetByDateRangeAsync(start.Value, end.Value, cancellationToken);
                return Ok(ApiResponse<IReadOnlyList<TimeEntryResponse>>.Ok(items));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("by-task/{taskItemId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TimeEntryResponse>>>> GetByTaskItemId(
            int taskItemId,
            CancellationToken cancellationToken)
        {
            try
            {
                var items = await _service.GetByTaskItemIdAsync(taskItemId, cancellationToken);
                return Ok(ApiResponse<IReadOnlyList<TimeEntryResponse>>.Ok(items));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("by-note/{noteId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TimeEntryResponse>>>> GetByNoteId(
            int noteId,
            CancellationToken cancellationToken)
        {
            try
            {
                var items = await _service.GetByNoteIdAsync(noteId, cancellationToken);
                return Ok(ApiResponse<IReadOnlyList<TimeEntryResponse>>.Ok(items));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = await _service.GetByIdAsync(id, cancellationToken);
                return Ok(ApiResponse<TimeEntryResponse>.Ok(item));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> Create(
            [FromBody] CreateTimeEntryRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = await _service.CreateAsync(request, cancellationToken);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = item.Id },
                    ApiResponse<TimeEntryResponse>.Ok(item, "Tiempo registrado."));
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<TimeEntryResponse>>> Update(
            int id,
            [FromBody] UpdateTimeEntryRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var item = await _service.UpdateAsync(id, request, cancellationToken);
                return Ok(ApiResponse<TimeEntryResponse>.Ok(item, "Registro de tiempo actualizado."));
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
                return Ok(ApiResponse<object>.Ok(null!, "Registro de tiempo eliminado."));
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