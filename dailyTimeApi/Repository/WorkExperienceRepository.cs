using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class WorkExperienceRepository : IWorkExperienceRepository
{
    private readonly AppDbContext _context;

    public WorkExperienceRepository(AppDbContext context) => _context = context;

    public Task<WorkExperience?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        DetailQuery(_context.WorkExperiences.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<WorkExperience?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default) =>
        DetailQuery(_context.WorkExperiences)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkExperience>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await DetailQuery(_context.WorkExperiences.AsNoTracking())
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobApplications.AsNoTracking()
            .AnyAsync(x => x.WorkExperienceId == id, cancellationToken);

    public async Task AddAsync(WorkExperience entity, CancellationToken cancellationToken = default) =>
        await _context.WorkExperiences.AddAsync(entity, cancellationToken);

    public void Update(WorkExperience entity) => _context.WorkExperiences.Update(entity);
    public void Remove(WorkExperience entity) => _context.WorkExperiences.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<WorkExperience> DetailQuery(IQueryable<WorkExperience> query) =>
        query
            .Include(x => x.Company)
            .Include(x => x.Position)
            .Include(x => x.Location)
            .Include(x => x.Field)
            .Include(x => x.Technologies)
                .ThenInclude(t => t.Technology);
}
