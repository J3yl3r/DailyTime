using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly AppDbContext _context;

    public TimeEntryRepository(AppDbContext context) => _context = context;

    public Task<TimeEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.TimeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        await _context.TimeEntries
            .AsNoTracking()
            .Where(x => x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TimeEntry>> GetByTaskItemIdAsync(
        int taskItemId, CancellationToken cancellationToken = default) =>
        await _context.TimeEntries
            .AsNoTracking()
            .Where(x => x.TaskItemId == taskItemId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TimeEntry>> GetByNoteIdAsync(
        int noteId, CancellationToken cancellationToken = default) =>
        await _context.TimeEntries
            .AsNoTracking()
            .Where(x => x.NoteId == noteId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TimeEntry entity, CancellationToken cancellationToken = default) =>
        await _context.TimeEntries.AddAsync(entity, cancellationToken);

    public void Update(TimeEntry entity) => _context.TimeEntries.Update(entity);

    public void Remove(TimeEntry entity) => _context.TimeEntries.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}