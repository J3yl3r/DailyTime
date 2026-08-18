using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class JobPortalScrapeLogRepository : IJobPortalScrapeLogRepository
{
    private readonly AppDbContext _context;

    public JobPortalScrapeLogRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(JobPortalScrapeLog entity, CancellationToken cancellationToken = default) =>
        await _context.JobPortalScrapeLogs.AddAsync(entity, cancellationToken);

    public void Update(JobPortalScrapeLog entity) =>
        _context.JobPortalScrapeLogs.Update(entity);

    public Task<JobPortalScrapeLog?> GetLatestOpenAsync(
        int jobPortalId, CancellationToken cancellationToken = default) =>
        _context.JobPortalScrapeLogs
            .Where(x => x.JobPortalId == jobPortalId && x.FinishedAt == null)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<JobPortalScrapeLog>> GetByPortalAsync(
        int jobPortalId, int take = 50, CancellationToken cancellationToken = default) =>
        await _context.JobPortalScrapeLogs.AsNoTracking()
            .Include(x => x.JobPortal)
            .Where(x => x.JobPortalId == jobPortalId)
            .OrderByDescending(x => x.StartedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
