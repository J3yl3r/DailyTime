using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/career-profile")]
public class CareerProfileController : ControllerBase
{
    private readonly ICareerProfileService _service;
    public CareerProfileController(ICareerProfileService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CareerProfileResponse>>> Get(
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetAsync(cancellationToken);
            return Ok(ApiResponse<CareerProfileResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<CareerProfileResponse>>> Upsert(
        [FromBody] UpsertCareerProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpsertAsync(request, cancellationToken);
            return Ok(ApiResponse<CareerProfileResponse>.Ok(item, "Perfil de postulación guardado."));
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
