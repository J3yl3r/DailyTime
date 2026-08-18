using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ITaskItemRepository
{
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetRootsByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem entity, CancellationToken cancellationToken = default);
    void Update(TaskItem entity);
    void Remove(TaskItem entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}