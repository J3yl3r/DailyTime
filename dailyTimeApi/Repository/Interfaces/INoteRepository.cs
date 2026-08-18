using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetRootsByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default);
    Task AddAsync(Note entity, CancellationToken cancellationToken = default);
    void Update(Note entity);
    void Remove(Note entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}