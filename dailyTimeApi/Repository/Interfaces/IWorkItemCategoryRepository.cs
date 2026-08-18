using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IWorkItemCategoryRepository
{
    Task<WorkItemCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItemCategory>> GetAllAsync(string? itemType = null, CancellationToken cancellationToken = default);
    Task<WorkItemCategory?> GetDefaultByItemTypeAsync(string itemType, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string itemType, string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(WorkItemCategory entity, CancellationToken cancellationToken = default);
    void Update(WorkItemCategory entity);
    void Remove(WorkItemCategory entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
