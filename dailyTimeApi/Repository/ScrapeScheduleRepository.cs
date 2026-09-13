using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class ScrapeScheduleRepository : IScrapeScheduleRepository
{
    private readonly AppDbContext _context;

    public ScrapeScheduleRepository(AppDbContext context) => _context = context;

    public Task<ScrapeSchedule?> GetAsync(CancellationToken cancellationToken = default) =>
        _context.ScrapeSchedules.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(ScrapeSchedule entity, CancellationToken cancellationToken = default) =>
        await _context.ScrapeSchedules.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
