using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IJobApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobApplication>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task AddAsync(JobApplication entity, CancellationToken cancellationToken = default);
    void Update(JobApplication entity);
    void Remove(JobApplication entity);
    Task<int> BulkDeleteAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
