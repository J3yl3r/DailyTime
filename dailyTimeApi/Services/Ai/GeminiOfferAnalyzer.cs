using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Services.Triage;
using Microsoft.Extensions.Options;

namespace dailyTimeApi.Services.Ai;

public interface IOfferAnalyzer
{
    bool IsConfigured { get; }

    Task<OfferAiAnalysis> AnalyzeAsync(
        JobOffer offer, TriageProfile profile, string model, CancellationToken cancellationToken = default);
}

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Clave de AI Studio. Va en user secrets (Gemini:ApiKey), nunca en appsettings versionados.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}

/// <summary>
/// Análisis con la API <c>generateContent</c> de Gemini: sin estado (no guarda conversaciones) y con
/// salida JSON validada por esquema.
/// </summary>
public class GeminiOfferAnalyzer : IOfferAnalyzer
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiOfferAnalyzer> _logger;

    public GeminiOfferAnalyzer(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiOfferAnalyzer> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<OfferAiAnalysis> AnalyzeAsync(
        JobOffer offer, TriageProfile profile, string model, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new OfferAiNotConfiguredException();

        var (status, body) = await SendAsync(model, BuildRequest(offer, profile, includeThinking: true), cancellationToken);
        if (status == 400 && body.Contains("thinking", StringComparison.OrdinalIgnoreCase))
        {
            // Algunos modelos no aceptan configurar el razonamiento: se reintenta con sus valores por defecto.
            _logger.LogInformation("El modelo {Model} no acepta thinkingConfig; reintentando sin él.", model);
            (status, body) = await SendAsync(model, BuildRequest(offer, profile, includeThinking: false), cancellationToken);
        }

        if (status is < 200 or >= 300)
            throw GeminiResponseParser.ParseError(status, body);

        return GeminiResponseParser.ParseAnalysis(body);
    }

    public static JsonObject BuildRequest(JobOffer offer, TriageProfile profile, bool includeThinking)
    {
        var generationConfig = new JsonObject
        {
            ["responseMimeType"] = "application/json",
            ["responseJsonSchema"] = JsonNode.Parse(OfferAiPrompt.ResponseSchemaJson)
        };
        if (includeThinking)
            generationConfig["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = "low" };

        return new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = OfferAiPrompt.SystemInstruction })
            },
            ["contents"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = OfferAiPrompt.BuildUserPrompt(offer, profile) })
            }),
            ["generationConfig"] = generationConfig
        };
    }

    private async Task<(int Status, string Body)> SendAsync(
        string model, JsonObject payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ((int)response.StatusCode, body);
        }
        catch (HttpRequestException ex)
        {
            throw new OfferAiException($"No se pudo conectar con Gemini: {ex.Message}", isFatal: true, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OfferAiException("Gemini no respondió a tiempo.", isFatal: true, ex);
        }
    }
}

public static class GeminiResponseParser
{
    public static OfferAiAnalysis ParseAnalysis(string responseBody)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(responseBody);
        }
        catch (JsonException ex)
        {
            throw new OfferAiException("La respuesta de Gemini no es JSON.", inner: ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("candidates", out var candidates)
                || candidates.ValueKind != JsonValueKind.Array
                || candidates.GetArrayLength() == 0)
            {
                var blockReason = root.TryGetProperty("promptFeedback", out var feedback)
                                  && feedback.TryGetProperty("blockReason", out var reason)
                    ? reason.GetString()
                    : null;
                throw new OfferAiException(blockReason is null
                    ? "Gemini no devolvió resultados."
                    : $"Gemini bloqueó la solicitud ({blockReason}).");
            }

            var candidate = candidates[0];
            var finishReason = candidate.TryGetProperty("finishReason", out var finish) ? finish.GetString() : null;
            var text = new StringBuilder();
            if (candidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    var isThought = part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True;
                    if (!isThought && part.TryGetProperty("text", out var partText))
                        text.Append(partText.GetString());
                }
            }

            if (text.Length == 0)
                throw new OfferAiException($"Gemini respondió sin texto (finishReason: {finishReason ?? "desconocido"}).");

            try
            {
                return OfferAiJson.Sanitize(OfferAiJson.Deserialize(text.ToString()));
            }
            catch (JsonException ex)
            {
                throw new OfferAiException(
                    $"El análisis de Gemini no es JSON válido (finishReason: {finishReason ?? "desconocido"}).", inner: ex);
            }
        }
    }

    public static OfferAiException ParseError(int statusCode, string body)
    {
        string? message = null;
        string? status = null;
        TimeSpan? retryAfter = null;
        var daily = false;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
                status = error.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (error.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                {
                    foreach (var detail in details.EnumerateArray())
                    {
                        var type = detail.TryGetProperty("@type", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                        if (type.EndsWith("google.rpc.RetryInfo", StringComparison.Ordinal)
                            && detail.TryGetProperty("retryDelay", out var delay)
                            && TryParseDuration(delay.GetString(), out var parsed))
                        {
                            retryAfter = parsed;
                        }

                        if (type.EndsWith("google.rpc.QuotaFailure", StringComparison.Ordinal)
                            && detail.TryGetProperty("violations", out var violations)
                            && violations.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var violation in violations.EnumerateArray())
                            {
                                var quotaId = violation.TryGetProperty("quotaId", out var q) ? q.GetString() : null;
                                if (quotaId?.Contains("PerDay", StringComparison.OrdinalIgnoreCase) == true)
                                    daily = true;
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Cuerpo no JSON: se usa el código HTTP.
        }

        var text = OfferAiJson.Truncate(message ?? $"HTTP {statusCode}", 300);
        if (statusCode == 429 || status == "RESOURCE_EXHAUSTED")
        {
            return new OfferAiQuotaException(
                daily ? $"Cuota diaria de Gemini agotada: {text}" : $"Límite de solicitudes de Gemini: {text}",
                retryAfter, daily);
        }

        var fatal = statusCode is 401 or 403 or 404
                    || (statusCode == 400 && text.Contains("API key", StringComparison.OrdinalIgnoreCase));
        return new OfferAiException($"Error de Gemini ({statusCode}): {text}", fatal);
    }

    private static bool TryParseDuration(string? value, out TimeSpan duration)
    {
        duration = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (!double.TryParse(value.Trim().TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            return false;
        duration = TimeSpan.FromSeconds(seconds);
        return true;
    }
}
