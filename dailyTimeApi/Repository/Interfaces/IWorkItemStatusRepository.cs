using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IWorkItemStatusRepository
{
    Task<WorkItemStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItemStatus>> GetAllAsync(string? itemType = null, CancellationToken cancellationToken = default);
    Task<WorkItemStatus?> GetDefaultByItemTypeAsync(string itemType, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(WorkItemStatus entity, CancellationToken cancellationToken = default);
    void Update(WorkItemStatus entity);
    void Remove(WorkItemStatus entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
