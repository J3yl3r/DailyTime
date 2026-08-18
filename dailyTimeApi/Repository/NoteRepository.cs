using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class NoteRepository : INoteRepository
{
    private readonly AppDbContext _context;

    public NoteRepository(AppDbContext context) => _context = context;

    public Task<Note?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Notes
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .Include(x => x.Project)
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Note>> GetRootsByDateRangeAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        await _context.Notes
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .Include(x => x.Project)
            .Include(x => x.Company)
            .Where(x => x.ParentNoteId == null && x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Note>> GetChildrenAsync(
        int parentId, CancellationToken cancellationToken = default) =>
        await _context.Notes
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .Include(x => x.Project)
            .Include(x => x.Company)
            .Where(x => x.ParentNoteId == parentId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Note entity, CancellationToken cancellationToken = default) =>
        await _context.Notes.AddAsync(entity, cancellationToken);

    public void Update(Note entity) => _context.Notes.Update(entity);

    public void Remove(Note entity) => _context.Notes.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
