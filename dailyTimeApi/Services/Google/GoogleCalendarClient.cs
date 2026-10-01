using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace dailyTimeApi.Services.Google;

public record GoogleCalendarSummary(string Id, string Name, string? TimeZoneId, bool IsPrimary);

public interface IGoogleCalendarClient
{
    /// <summary>
    /// Eventos de la ventana pedida. <paramref name="updatedMin"/> limita a los modificados
    /// desde ese momento (incluye los borrados), que es como se hace el sondeo incremental.
    /// </summary>
    Task<JsonObject> ListEventsAsync(
        string accessToken,
        string calendarId,
        DateTime timeMin,
        DateTime timeMax,
        DateTime? updatedMin,
        string? pageToken,
        CancellationToken cancellationToken = default);

    Task<JsonObject> InsertEventAsync(
        string accessToken, string calendarId, JsonObject body, CancellationToken cancellationToken = default);

    Task<JsonObject> PatchEventAsync(
        string accessToken, string calendarId, string eventId, JsonObject body,
        CancellationToken cancellationToken = default);

    /// <summary>Devuelve false si el evento ya no existía en Google.</summary>
    Task<bool> DeleteEventAsync(
        string accessToken, string calendarId, string eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoogleCalendarSummary>> ListCalendarsAsync(
        string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Paleta de eventos de Google: colorId -> color de fondo en hexadecimal.</summary>
    Task<IReadOnlyDictionary<string, string>> GetEventColorsAsync(
        string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Color por defecto del calendario, el que usan los eventos sin color propio.</summary>
    Task<string?> GetCalendarColorAsync(
        string accessToken, string calendarId, CancellationToken cancellationToken = default);
}

/// <summary>API REST de Google Calendar v3, sin SDK: las llamadas que hacen falta son pocas.</summary>
public class GoogleCalendarClient : IGoogleCalendarClient
{
    private const int PageSize = 250;

    private readonly HttpClient _http;
    private readonly GoogleCalendarOptions _options;

    public GoogleCalendarClient(HttpClient http, IOptions<GoogleCalendarOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public Task<JsonObject> ListEventsAsync(
        string accessToken,
        string calendarId,
        DateTime timeMin,
        DateTime timeMax,
        DateTime? updatedMin,
        string? pageToken,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            // singleEvents expande las series repetidas: cada ocurrencia es un bloque del día.
            ["singleEvents"] = "true",
            ["showDeleted"] = "true",
            ["maxResults"] = PageSize.ToString(CultureInfo.InvariantCulture),
            ["timeMin"] = Rfc3339(timeMin),
            ["timeMax"] = Rfc3339(timeMax)
        };
        if (updatedMin.HasValue)
            query["updatedMin"] = Rfc3339(updatedMin.Value);
        if (!string.IsNullOrWhiteSpace(pageToken))
            query["pageToken"] = pageToken!;

        var url = EventsUrl(calendarId) + "?" + string.Join(
            "&", query.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
        return SendAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);
    }

    public Task<JsonObject> InsertEventAsync(
        string accessToken, string calendarId, JsonObject body, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, EventsUrl(calendarId), accessToken, body, cancellationToken);

    public Task<JsonObject> PatchEventAsync(
        string accessToken, string calendarId, string eventId, JsonObject body,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Patch, EventsUrl(calendarId) + "/" + Uri.EscapeDataString(eventId),
            accessToken, body, cancellationToken);

    public async Task<bool> DeleteEventAsync(
        string accessToken, string calendarId, string eventId, CancellationToken cancellationToken = default)
    {
        var url = EventsUrl(calendarId) + "/" + Uri.EscapeDataString(eventId);
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);

        // 404/410: el evento ya no está en Google; el borrado local ya está cumplido.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            return false;
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw Fail((int)response.StatusCode, body);
        }
        return true;
    }

    public async Task<IReadOnlyList<GoogleCalendarSummary>> ListCalendarsAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        var url = _options.ApiBaseUrl.TrimEnd('/') + "/users/me/calendarList?minAccessRole=writer&maxResults=250";
        var json = await SendAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);
        var items = json["items"] as JsonArray;
        if (items is null)
            return Array.Empty<GoogleCalendarSummary>();

        return items
            .OfType<JsonObject>()
            .Select(item => new GoogleCalendarSummary(
                item["id"]?.GetValue<string>() ?? string.Empty,
                item["summary"]?.GetValue<string>() ?? item["id"]?.GetValue<string>() ?? "(sin nombre)",
                item["timeZone"]?.GetValue<string>(),
                item["primary"]?.GetValue<bool>() ?? false))
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEventColorsAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        var url = _options.ApiBaseUrl.TrimEnd('/') + "/colors";
        var json = await SendAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);

        var paleta = new Dictionary<string, string>(StringComparer.Ordinal);
        if (json["event"] is JsonObject colores)
        {
            foreach (var entrada in colores)
            {
                var fondo = entrada.Value?["background"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(fondo))
                    paleta[entrada.Key] = fondo!;
            }
        }
        return paleta;
    }

    public async Task<string?> GetCalendarColorAsync(
        string accessToken, string calendarId, CancellationToken cancellationToken = default)
    {
        var url = _options.ApiBaseUrl.TrimEnd('/') +
                  "/users/me/calendarList/" + Uri.EscapeDataString(calendarId);
        var json = await SendAsync(HttpMethod.Get, url, accessToken, null, cancellationToken);
        return json["backgroundColor"]?.GetValue<string>();
    }

    private string EventsUrl(string calendarId) =>
        _options.ApiBaseUrl.TrimEnd('/') + "/calendars/" + Uri.EscapeDataString(calendarId) + "/events";

    private async Task<JsonObject> SendAsync(
        HttpMethod method, string url, string accessToken, JsonObject? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw Fail((int)response.StatusCode, payload);

        return JsonNode.Parse(payload) as JsonObject
            ?? throw new GoogleApiException(
                (int)response.StatusCode, payload, "Respuesta de Google Calendar ilegible.");
    }

    private static GoogleApiException Fail(int status, string body) =>
        new(status, body, GoogleOAuthClient.DescribeError(body, status));

    internal static string Rfc3339(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
