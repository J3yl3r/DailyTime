using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class OfferTriageConfigRepository : IOfferTriageConfigRepository
{
    private readonly AppDbContext _context;

    public OfferTriageConfigRepository(AppDbContext context) => _context = context;

    public Task<OfferTriageConfig?> GetAsync(CancellationToken cancellationToken = default) =>
        _context.OfferTriageConfigs.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(OfferTriageConfig entity, CancellationToken cancellationToken = default) =>
        await _context.OfferTriageConfigs.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
