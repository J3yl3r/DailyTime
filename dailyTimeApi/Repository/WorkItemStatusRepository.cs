using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class WorkItemStatusRepository : IWorkItemStatusRepository
{
    private readonly AppDbContext _context;

    public WorkItemStatusRepository(AppDbContext context) => _context = context;

    public Task<WorkItemStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.WorkItemStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkItemStatus>> GetAllAsync(
        string? itemType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkItemStatuses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(itemType))
            query = query.Where(x => x.ItemType == itemType);

        return await query
            .OrderBy(x => x.ItemType)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkItemStatus?> GetDefaultByItemTypeAsync(
        string itemType,
        CancellationToken cancellationToken = default) =>
        _context.WorkItemStatuses
            .AsNoTracking()
            .Where(x => x.ItemType == itemType)
            .OrderBy(x => x.IsFinal)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        var usedByTasks = await _context.TaskItems
            .AsNoTracking()
            .AnyAsync(x => x.StatusId == id, cancellationToken);

        if (usedByTasks) return true;

        return await _context.Notes
            .AsNoTracking()
            .AnyAsync(x => x.StatusId == id, cancellationToken);
    }

    public async Task AddAsync(WorkItemStatus entity, CancellationToken cancellationToken = default) =>
        await _context.WorkItemStatuses.AddAsync(entity, cancellationToken);

    public void Update(WorkItemStatus entity) => _context.WorkItemStatuses.Update(entity);

    public void Remove(WorkItemStatus entity) => _context.WorkItemStatuses.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
