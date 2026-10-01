using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class GoogleCalendarRepository : IGoogleCalendarRepository
{
    private readonly AppDbContext _context;

    public GoogleCalendarRepository(AppDbContext context) => _context = context;

    public Task<GoogleCalendarAccount?> GetAccountAsync(CancellationToken cancellationToken = default) =>
        _context.GoogleCalendarAccounts.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAccountAsync(GoogleCalendarAccount account, CancellationToken cancellationToken = default) =>
        await _context.GoogleCalendarAccounts.AddAsync(account, cancellationToken);

    public void RemoveAccount(GoogleCalendarAccount account) =>
        _context.GoogleCalendarAccounts.Remove(account);

    public async Task<IReadOnlyList<TaskItem>> GetTasksInWindowAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        await _context.TaskItems
            .Where(x => x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Note>> GetNotesInWindowAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        await _context.Notes
            .Where(x => x.WorkDate != null && x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<TaskItem?> GetTaskByEventIdAsync(
        string googleEventId, CancellationToken cancellationToken = default) =>
        _context.TaskItems.FirstOrDefaultAsync(x => x.GoogleEventId == googleEventId, cancellationToken);

    public Task<Note?> GetNoteByEventIdAsync(
        string googleEventId, CancellationToken cancellationToken = default) =>
        _context.Notes.FirstOrDefaultAsync(x => x.GoogleEventId == googleEventId, cancellationToken);

    public Task<TaskItem?> GetTaskByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.TaskItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Note?> GetNoteByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddTaskAsync(TaskItem entity, CancellationToken cancellationToken = default) =>
        await _context.TaskItems.AddAsync(entity, cancellationToken);

    public void RemoveTask(TaskItem entity) => _context.TaskItems.Remove(entity);

    public void RemoveNote(Note entity) => _context.Notes.Remove(entity);

    public Task<bool> TaskHasChildrenAsync(int id, CancellationToken cancellationToken = default) =>
        _context.TaskItems.AnyAsync(x => x.ParentTaskId == id, cancellationToken);

    public Task<bool> NoteHasChildrenAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Notes.AnyAsync(x => x.ParentNoteId == id, cancellationToken);

    public async Task<IReadOnlyList<GoogleSyncDeletion>> GetPendingDeletionsAsync(
        CancellationToken cancellationToken = default) =>
        await _context.GoogleSyncDeletions
            .OrderBy(x => x.DeletedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

    public async Task AddDeletionAsync(
        GoogleSyncDeletion deletion, CancellationToken cancellationToken = default) =>
        await _context.GoogleSyncDeletions.AddAsync(deletion, cancellationToken);

    public void RemoveDeletion(GoogleSyncDeletion deletion) =>
        _context.GoogleSyncDeletions.Remove(deletion);

    public async Task<int> ClearSyncMarksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _context.TaskItems
            .Where(x => x.GoogleEventId != null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.GoogleEventId, (string?)null)
                    .SetProperty(x => x.GoogleEtag, (string?)null)
                    .SetProperty(x => x.GoogleSyncedAt, (DateTime?)null)
                    .SetProperty(x => x.GoogleUpdatedAt, (DateTime?)null),
                cancellationToken);

        var notes = await _context.Notes
            .Where(x => x.GoogleEventId != null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.GoogleEventId, (string?)null)
                    .SetProperty(x => x.GoogleEtag, (string?)null)
                    .SetProperty(x => x.GoogleSyncedAt, (DateTime?)null)
                    .SetProperty(x => x.GoogleUpdatedAt, (DateTime?)null),
                cancellationToken);

        return tasks + notes;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
