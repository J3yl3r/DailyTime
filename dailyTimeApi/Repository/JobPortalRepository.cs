using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class JobPortalRepository : IJobPortalRepository
{
    private readonly AppDbContext _context;

    public JobPortalRepository(AppDbContext context) => _context = context;

    public Task<JobPortal?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobPortals.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobPortal>> GetAllAsync(
        bool? onlyActive = null,
        bool queuedOnly = false,
        bool autoOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobPortals.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        if (queuedOnly)
            query = query.Where(x => x.IsActive && x.LastRunStatus == "queued_playwright");
        if (autoOnly)
            query = query.Where(x => x.IsActive && x.AutoScrapeEnabled);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.JobPortals.AsNoTracking()
            .Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(JobPortal entity, CancellationToken cancellationToken = default) =>
        await _context.JobPortals.AddAsync(entity, cancellationToken);

    public void Update(JobPortal entity) => _context.JobPortals.Update(entity);

    public void Remove(JobPortal entity) => _context.JobPortals.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
