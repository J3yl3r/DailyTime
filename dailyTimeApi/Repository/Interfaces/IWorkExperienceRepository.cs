using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IWorkExperienceRepository
{
    Task<WorkExperience?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkExperience?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkExperience>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(WorkExperience entity, CancellationToken cancellationToken = default);
    void Update(WorkExperience entity);
    void Remove(WorkExperience entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
