using System.Text.Encodings.Web;
using dailyTimeApi.Common;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Services.Google;
using Microsoft.AspNetCore.Mvc;

namespace dailyTimeApi.Controllers;

/// <summary>Conexión y sincronización bidireccional con Google Calendar.</summary>
[ApiController]
[Route("api/google-calendar")]
public class GoogleCalendarController : ControllerBase
{
    private readonly IGoogleCalendarService _service;
    private readonly ILogger<GoogleCalendarController> _logger;

    public GoogleCalendarController(IGoogleCalendarService service, ILogger<GoogleCalendarController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<GoogleCalendarStatusResponse>>> GetStatus(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ApiResponse<GoogleCalendarStatusResponse>.Ok(
                await _service.GetStatusAsync(cancellationToken)));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>URL de consentimiento de Google. La web la abre en una ventana aparte.</summary>
    [HttpGet("auth-url")]
    public ActionResult<ApiResponse<GoogleAuthUrlResponse>> GetAuthUrl()
    {
        try
        {
            return Ok(ApiResponse<GoogleAuthUrlResponse>.Ok(_service.BuildAuthUrl()));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>
    /// Vuelta del consentimiento de Google. La abre el navegador, no la web: responde HTML.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return CallbackPage(false, "Google canceló la conexión: " + error);

        try
        {
            var status = await _service.CompleteConnectAsync(code ?? string.Empty, state ?? string.Empty, cancellationToken);
            return CallbackPage(true, "Cuenta conectada: " + status.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo el callback de Google Calendar.");
            return CallbackPage(false, ex.Message);
        }
    }

    [HttpPost("disconnect")]
    public async Task<ActionResult<ApiResponse<GoogleCalendarStatusResponse>>> Disconnect(
        CancellationToken cancellationToken)
    {
        try
        {
            await _service.DisconnectAsync(cancellationToken);
            return Ok(ApiResponse<GoogleCalendarStatusResponse>.Ok(
                await _service.GetStatusAsync(cancellationToken), "Cuenta de Google desconectada."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<GoogleCalendarStatusResponse>>> UpdateSettings(
        [FromBody] UpdateGoogleCalendarSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ApiResponse<GoogleCalendarStatusResponse>.Ok(
                await _service.UpdateSettingsAsync(request, cancellationToken), "Ajustes guardados."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Sincroniza ya y espera el resultado.</summary>
    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<GoogleSyncRunResponse>>> SyncNow(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SyncNowAsync(cancellationToken);
            return Ok(ApiResponse<GoogleSyncRunResponse>.Ok(
                result,
                result.Message ?? $"{result.Pushed} enviados y {result.Pulled} traídos."));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    /// <summary>Calendarios de la cuenta en los que se puede escribir.</summary>
    [HttpGet("calendars")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GoogleCalendarListItemResponse>>>> GetCalendars(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ApiResponse<IReadOnlyList<GoogleCalendarListItemResponse>>.Ok(
                await _service.ListCalendarsAsync(cancellationToken)));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    private ContentResult CallbackPage(bool success, string message)
    {
        var title = success ? "Listo" : "No se pudo conectar";
        var color = success ? "#22C55E" : "#EF4444";
        var encoded = HtmlEncoder.Default.Encode(message);
        var html =
            "<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\">" +
            "<title>DailyTime · Google Calendar</title>" +
            "<style>body{font-family:system-ui,sans-serif;background:#0f172a;color:#e2e8f0;" +
            "display:flex;align-items:center;justify-content:center;height:100vh;margin:0}" +
            ".card{max-width:26rem;padding:2rem;border-radius:1rem;background:#1e293b;text-align:center}" +
            "h1{font-size:1.25rem;margin:0 0 .75rem;color:" + color + "}" +
            "p{margin:0;font-size:.9rem;line-height:1.5;color:#94a3b8}</style></head>" +
            "<body><div class=\"card\"><h1>" + title + "</h1><p>" + encoded + "</p>" +
            "<p style=\"margin-top:1rem\">Ya puedes cerrar esta ventana y volver a DailyTime.</p></div>" +
            "<script>setTimeout(function(){window.close();},2500);</script></body></html>";

        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 200 };
    }

    private ActionResult HandleError(Exception ex) => ex switch
    {
        NotFoundException => NotFound(ApiResponse<object>.Fail(ex.Message)),
        ValidationException ve => BadRequest(ApiResponse<object>.Fail(ex.Message, ve.Errors)),
        GoogleReauthRequiredException => BadRequest(ApiResponse<object>.Fail(ex.Message)),
        GoogleApiException => BadRequest(ApiResponse<object>.Fail(ex.Message)),
        _ => StatusCode(500, ApiResponse<object>.Fail(
            string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                ? $"Error interno del servidor: {ex.Message}"
                : $"Error interno del servidor: {ex.InnerException.Message}"))
    };
}
