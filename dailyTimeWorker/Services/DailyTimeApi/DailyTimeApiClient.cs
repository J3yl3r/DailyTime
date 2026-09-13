using System.Net.Http.Json;
using System.Text.Json;
using dailyTimeWorker.Models;

namespace dailyTimeWorker.Services.DailyTimeApi;

public interface IDailyTimeApiClient
{
    Task<IReadOnlyList<JobPortalDto>> GetQueuedPortalsAsync(CancellationToken cancellationToken = default);
    /// <summary>Portales activos marcados como Automática.</summary>
    Task<IReadOnlyList<JobPortalDto>> GetAutoPortalsAsync(CancellationToken cancellationToken = default);
    Task<JobPortalDto?> GetPortalAsync(int id, CancellationToken cancellationToken = default);
    Task<ScrapeScheduleDto> GetScheduleAsync(CancellationToken cancellationToken = default);
    Task MarkScheduleSlotAsync(MarkScrapeSlotRequest request, CancellationToken cancellationToken = default);
    Task UpdateScrapeRunAsync(int id, UpdateScrapeRunRequest request, CancellationToken cancellationToken = default);
    Task<UpsertJobOffersResponse> UpsertOffersAsync(
        UpsertJobOffersRequest request, CancellationToken cancellationToken = default);
}

public class DailyTimeApiClient : IDailyTimeApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<DailyTimeApiClient> _logger;

    public DailyTimeApiClient(HttpClient http, ILogger<DailyTimeApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobPortalDto>> GetQueuedPortalsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("api/job-portals?queuedOnly=true", cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<JobPortalDto>>>(
            JsonOptions, cancellationToken);
        return payload?.Data ?? [];
    }

    public async Task<IReadOnlyList<JobPortalDto>> GetAutoPortalsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("api/job-portals?autoOnly=true", cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<JobPortalDto>>>(
            JsonOptions, cancellationToken);
        return payload?.Data ?? [];
    }

    public async Task<ScrapeScheduleDto> GetScheduleAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("api/scrape-schedule", cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ScrapeScheduleDto>>(
            JsonOptions, cancellationToken);
        return payload?.Data
            ?? throw new InvalidOperationException("La API no devolvió el horario de captura.");
    }

    public async Task MarkScheduleSlotAsync(
        MarkScrapeSlotRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync("api/scrape-schedule/last-slot", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("No se pudo registrar la franja {Slot} ({Status}): {Body}",
                request.SlotAt, request.Status, body);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<JobPortalDto?> GetPortalAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"api/job-portals/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JobPortalDto>>(
            JsonOptions, cancellationToken);
        return payload?.Data;
    }

    public async Task UpdateScrapeRunAsync(
        int id, UpdateScrapeRunRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/job-portals/{id}/run-status", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("No se pudo actualizar run-status del portal {Id}: {Body}", id, body);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<UpsertJobOffersResponse> UpsertOffersAsync(
        UpsertJobOffersRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/job-offers/upsert", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UpsertJobOffersResponse>>(
            JsonOptions, cancellationToken);
        return payload?.Data ?? new UpsertJobOffersResponse();
    }
}
