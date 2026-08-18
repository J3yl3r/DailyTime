using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IJobPortalScrapeLogRepository
{
    Task AddAsync(JobPortalScrapeLog entity, CancellationToken cancellationToken = default);
    void Update(JobPortalScrapeLog entity);
    Task<JobPortalScrapeLog?> GetLatestOpenAsync(int jobPortalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobPortalScrapeLog>> GetByPortalAsync(
        int jobPortalId, int take = 50, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
