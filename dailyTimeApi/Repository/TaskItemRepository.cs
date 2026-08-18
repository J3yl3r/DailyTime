using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly AppDbContext _context;

    public TaskItemRepository(AppDbContext context) => _context = context;

    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.TaskItems
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .Include(x => x.Project)
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetRootsByDateRangeAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
        await _context.TaskItems
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .Include(x => x.Project)
            .Include(x => x.Company)
            .Where(x => x.ParentTaskId == null && x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetChildrenAsync(
        int parentId, CancellationToken cancellationToken = default) =>
        await _context.TaskItems
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Category)
            .Include(x => x.Person)
            .Include(x => x.Project)
            .Include(x => x.Company)
            .Where(x => x.ParentTaskId == parentId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskItem entity, CancellationToken cancellationToken = default) =>
        await _context.TaskItems.AddAsync(entity, cancellationToken);

    public void Update(TaskItem entity) => _context.TaskItems.Update(entity);

    public void Remove(TaskItem entity) => _context.TaskItems.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
