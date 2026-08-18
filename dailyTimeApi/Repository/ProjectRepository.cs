using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context) => _context = context;

    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.TaskItems.AsNoTracking()
            .AnyAsync(x => x.ProjectId == id, cancellationToken))
            return true;

        return await _context.Notes.AsNoTracking()
            .AnyAsync(x => x.ProjectId == id, cancellationToken);
    }

    public async Task AddAsync(Project entity, CancellationToken cancellationToken = default) =>
        await _context.Projects.AddAsync(entity, cancellationToken);

    public void Update(Project entity) => _context.Projects.Update(entity);
    public void Remove(Project entity) => _context.Projects.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
