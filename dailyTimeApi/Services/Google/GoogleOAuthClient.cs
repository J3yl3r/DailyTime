using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace dailyTimeApi.Services.Google;

public record GoogleTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public interface IGoogleOAuthClient
{
    bool IsConfigured { get; }
    string RedirectUri { get; }
    string BuildAuthUrl(string state);
    Task<GoogleTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<GoogleTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<string?> GetEmailAsync(string accessToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Flujo OAuth 2.0 de aplicación instalada: el navegador vuelve a un redirect de loopback
/// servido por esta misma API. Se pide <c>access_type=offline</c> para obtener refresh token.
/// </summary>
public class GoogleOAuthClient : IGoogleOAuthClient
{
    /// <summary>Lectura de calendarios, escritura de eventos y el correo de la cuenta.</summary>
    public const string Scopes =
        "https://www.googleapis.com/auth/calendar.events " +
        "https://www.googleapis.com/auth/calendar.readonly " +
        "openid email";

    private readonly HttpClient _http;
    private readonly GoogleCalendarOptions _options;

    public GoogleOAuthClient(HttpClient http, IOptions<GoogleCalendarOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public bool IsConfigured => _options.IsConfigured;

    public string RedirectUri => _options.RedirectUri;

    public string BuildAuthUrl(string state)
    {
        EnsureConfigured();
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId!,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = Scopes,
            ["access_type"] = "offline",
            // Fuerza la pantalla de consentimiento: sin ella Google omite el refresh token
            // cuando la cuenta ya autorizó la app antes.
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = state
        };
        var encoded = string.Join(
            "&",
            query.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
        return _options.AuthUrl + "?" + encoded;
    }

    public async Task<GoogleTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var form = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["redirect_uri"] = _options.RedirectUri,
            ["grant_type"] = "authorization_code"
        };
        var json = await PostFormAsync(_options.TokenUrl, form, cancellationToken);
        var refresh = json["refresh_token"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(refresh))
            throw new GoogleReauthRequiredException(
                "Google no devolvió refresh token. Quita el acceso de la app en tu cuenta de Google y vuelve a conectar.");
        return ToTokens(json, refresh);
    }

    public async Task<GoogleTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var form = new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = _options.ClientId!,
            ["client_secret"] = _options.ClientSecret!,
            ["grant_type"] = "refresh_token"
        };

        try
        {
            var json = await PostFormAsync(_options.TokenUrl, form, cancellationToken);
            return ToTokens(json, refreshToken);
        }
        catch (GoogleApiException ex) when (ex.StatusCode is 400 or 401)
        {
            // invalid_grant: se revocó el acceso, cambió la contraseña, o la app sigue en modo
            // "Testing" en Google Cloud y el refresh token caducó a los 7 días.
            throw new GoogleReauthRequiredException(
                "Google rechazó el refresh token. Vuelve a conectar la cuenta desde el calendario.");
        }
    }

    public async Task<string?> GetEmailAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        return JsonNode.Parse(body)?["email"]?.GetValue<string>();
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
        // Un token ya revocado responde 400: desconectar igual es lo correcto.
        using var response = await _http.PostAsync(_options.RevokeUrl, content, cancellationToken);
        _ = response;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new GoogleApiException(
                0, string.Empty, "Faltan Google:ClientId y Google:ClientSecret en user secrets.");
    }

    private async Task<JsonObject> PostFormAsync(
        string url, Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new GoogleApiException((int)response.StatusCode, body, DescribeError(body, (int)response.StatusCode));
        return JsonNode.Parse(body) as JsonObject
            ?? throw new GoogleApiException((int)response.StatusCode, body, "Respuesta OAuth de Google ilegible.");
    }

    private static GoogleTokens ToTokens(JsonObject json, string refreshToken)
    {
        var accessToken = json["access_token"]?.GetValue<string>()
            ?? throw new GoogleApiException(0, json.ToJsonString(), "Google no devolvió access token.");
        var expiresIn = json["expires_in"]?.GetValue<int>() ?? 3600;
        return new GoogleTokens(accessToken, refreshToken, DateTime.UtcNow.AddSeconds(expiresIn));
    }

    internal static string DescribeError(string body, int status)
    {
        try
        {
            var node = JsonNode.Parse(body);
            var description = node?["error_description"]?.GetValue<string>();
            var nested = node?["error"]?["message"]?.GetValue<string>();
            var error = node?["error"] as JsonValue;
            var detail = description ?? nested ?? error?.ToString();
            if (!string.IsNullOrWhiteSpace(detail))
                return "Google respondió " + status + ": " + detail;
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
        return "Google respondió " + status + ".";
    }
}
