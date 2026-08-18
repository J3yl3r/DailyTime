using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

[ApiController]
public class VaultPasswordsController : ControllerBase
{
    private readonly IVaultPasswordService _service;
    public VaultPasswordsController(IVaultPasswordService service) => _service = service;

    [HttpGet("api/vault/accounts/{accountId:int}/passwords")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VaultPasswordResponse>>>> GetByAccount(
        int accountId, CancellationToken cancellationToken)
    {
        try
        {
            var items = await _service.GetByAccountIdAsync(accountId, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<VaultPasswordResponse>>.Ok(items));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("api/vault/passwords/{id:int}")]
    public async Task<ActionResult<ApiResponse<VaultPasswordResponse>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<VaultPasswordResponse>.Ok(item));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("api/vault/accounts/{accountId:int}/passwords")]
    public async Task<ActionResult<ApiResponse<VaultPasswordResponse>>> Create(
        int accountId, [FromBody] CreateVaultPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.CreateAsync(accountId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse<VaultPasswordResponse>.Ok(item, "Contraseña creada."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("api/vault/passwords/{id:int}")]
    public async Task<ActionResult<ApiResponse<VaultPasswordResponse>>> Update(
        int id, [FromBody] UpdateVaultPasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(ApiResponse<VaultPasswordResponse>.Ok(item, "Contraseña actualizada."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpDelete("api/vault/passwords/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null!, "Contraseña eliminada."));
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
