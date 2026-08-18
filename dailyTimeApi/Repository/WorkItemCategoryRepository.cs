using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class WorkItemCategoryRepository : IWorkItemCategoryRepository
{
    private readonly AppDbContext _context;

    public WorkItemCategoryRepository(AppDbContext context) => _context = context;

    public Task<WorkItemCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.WorkItemCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkItemCategory>> GetAllAsync(
        string? itemType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.WorkItemCategories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(itemType))
            query = query.Where(x => x.ItemType == itemType);

        return await query.OrderBy(x => x.ItemType)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkItemCategory?> GetDefaultByItemTypeAsync(
        string itemType, CancellationToken cancellationToken = default) =>
        _context.WorkItemCategories.AsNoTracking()
            .Where(x => x.ItemType == itemType && x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string itemType, string name, int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkItemCategories.AsNoTracking()
            .Where(x => x.ItemType == itemType && x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.TaskItems.AsNoTracking()
            .AnyAsync(x => x.CategoryId == id, cancellationToken))
            return true;

        return await _context.Notes.AsNoTracking()
            .AnyAsync(x => x.CategoryId == id, cancellationToken);
    }

    public async Task AddAsync(WorkItemCategory entity, CancellationToken cancellationToken = default) =>
        await _context.WorkItemCategories.AddAsync(entity, cancellationToken);

    public void Update(WorkItemCategory entity) => _context.WorkItemCategories.Update(entity);
    public void Remove(WorkItemCategory entity) => _context.WorkItemCategories.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
