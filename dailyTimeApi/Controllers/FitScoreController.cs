using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
[Route("api/fit-score")]
public class FitScoreController : ControllerBase
{
    private readonly IFitScoreService _service;

    public FitScoreController(IFitScoreService service) => _service = service;

    /// <summary>
    /// Evalúa encaje entre una experiencia (CV) y una oferta capturada.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<FitScoreResponse>>> Evaluate(
        [FromBody] EvaluateFitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.EvaluateAsync(request, cancellationToken);
            return Ok(ApiResponse<FitScoreResponse>.Ok(result));
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
