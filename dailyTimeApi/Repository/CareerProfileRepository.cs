using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerProfileRepository : ICareerProfileRepository
{
    private readonly AppDbContext _context;

    public CareerProfileRepository(AppDbContext context) => _context = context;

    public Task<CareerProfile?> GetAsync(CancellationToken cancellationToken = default) =>
        DetailQuery(_context.CareerProfiles.AsNoTracking())
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<CareerProfile?> GetTrackedAsync(CancellationToken cancellationToken = default) =>
        DetailQuery(_context.CareerProfiles)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(CareerProfile entity, CancellationToken cancellationToken = default) =>
        await _context.CareerProfiles.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<CareerProfile> DetailQuery(IQueryable<CareerProfile> query) =>
        query
            .Include(x => x.Links)
            .Include(x => x.Languages)
            .Include(x => x.Countries)
            .Include(x => x.Stacks)
            .Include(x => x.Strengths)
            .Include(x => x.Education)
            .Include(x => x.Certifications)
            .Include(x => x.CoverLetters);
}
