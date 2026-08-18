using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Project entity, CancellationToken cancellationToken = default);
    void Update(Project entity);
    void Remove(Project entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
