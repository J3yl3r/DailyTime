using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IJobPortalService
{
    Task<IReadOnlyList<JobPortalResponse>> GetAllAsync(
        bool? onlyActive = null,
        bool queuedOnly = false,
        bool autoOnly = false,
        CancellationToken cancellationToken = default);
    Task<JobPortalResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<JobPortalResponse> SetAutoScrapeAsync(int id, bool enabled, CancellationToken cancellationToken = default);
    Task<JobPortalResponse> CreateAsync(CreateJobPortalRequest request, CancellationToken cancellationToken = default);
    Task<JobPortalResponse> UpdateAsync(int id, UpdateJobPortalRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<JobPortalResponse> MarkScrapeQueuedAsync(int id, CancellationToken cancellationToken = default);
    Task<JobPortalResponse> UpdateRunStatusAsync(
        int id, UpdateJobPortalRunStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobPortalScrapeLogResponse>> GetScrapeLogsAsync(
        int portalId, int take = 50, CancellationToken cancellationToken = default);
}
