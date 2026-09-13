using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IJobPortalRepository
{
    Task<JobPortal?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobPortal>> GetAllAsync(
        bool? onlyActive = null,
        bool queuedOnly = false,
        bool autoOnly = false,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(JobPortal entity, CancellationToken cancellationToken = default);
    void Update(JobPortal entity);
    void Remove(JobPortal entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
