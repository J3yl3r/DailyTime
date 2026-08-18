using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/people")]
public class PeopleController : ControllerBase
{
    private readonly IPersonService _service;
    public PeopleController(IPersonService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonResponse>>>> GetAll(
        [FromQuery] bool? onlyActive, CancellationToken cancellationToken)
    {
        try
        {
            var items = await _service.GetAllAsync(onlyActive, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<PersonResponse>>.Ok(items));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PersonResponse>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<PersonResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PersonResponse>>> Create(
        [FromBody] CreatePersonRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse<PersonResponse>.Ok(item, "Persona creada."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PersonResponse>>> Update(
        int id, [FromBody] UpdatePersonRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<PersonResponse>.Ok(item, "Persona actualizada."));
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
            return Ok(ApiResponse<object>.Ok(null!, "Persona eliminada."));
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
