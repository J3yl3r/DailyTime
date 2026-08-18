using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimeEntry>> GetByTaskItemIdAsync(int taskItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimeEntry>> GetByNoteIdAsync(int noteId, CancellationToken cancellationToken = default);
    Task AddAsync(TimeEntry entity, CancellationToken cancellationToken = default);
    void Update(TimeEntry entity);
    void Remove(TimeEntry entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}