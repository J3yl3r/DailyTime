using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly AppDbContext _context;

    public JobApplicationRepository(AppDbContext context) => _context = context;

    public Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        DetailQuery(_context.JobApplications.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobApplication>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await DetailQuery(_context.JobApplications.AsNoTracking())
            .OrderByDescending(x => x.AppliedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobApplication entity, CancellationToken cancellationToken = default) =>
        await _context.JobApplications.AddAsync(entity, cancellationToken);

    public void Update(JobApplication entity) => _context.JobApplications.Update(entity);
    public void Remove(JobApplication entity) => _context.JobApplications.Remove(entity);

    public Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default) =>
        _context.JobApplications.AsNoTracking()
            .AnyAsync(x => x.Url == url, cancellationToken);

    public async Task<int> BulkDeleteAsync(
        IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        return await _context.JobApplications
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<JobApplication> DetailQuery(IQueryable<JobApplication> query) =>
        query
            .Include(x => x.Company)
            .Include(x => x.Position)
            .Include(x => x.Location)
            .Include(x => x.Field)
            .Include(x => x.Status)
            .Include(x => x.WorkExperience!)
                .ThenInclude(w => w.Company)
            .Include(x => x.WorkExperience!)
                .ThenInclude(w => w.Position);
}
