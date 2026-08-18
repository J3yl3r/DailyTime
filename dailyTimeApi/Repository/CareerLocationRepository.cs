using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerLocationRepository : ICareerLocationRepository
{
    private readonly AppDbContext _context;

    public CareerLocationRepository(AppDbContext context) => _context = context;

    public Task<CareerLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerLocations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CareerLocation>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerLocations.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerLocations.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<CareerLocation?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return _context.CareerLocations
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerLocations.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.WorkExperiences.AsNoTracking()
            .AnyAsync(x => x.LocationId == id, cancellationToken))
            return true;
        return await _context.JobApplications.AsNoTracking()
            .AnyAsync(x => x.LocationId == id, cancellationToken);
    }

    public async Task AddAsync(CareerLocation entity, CancellationToken cancellationToken = default) =>
        await _context.CareerLocations.AddAsync(entity, cancellationToken);

    public void Update(CareerLocation entity) => _context.CareerLocations.Update(entity);
    public void Remove(CareerLocation entity) => _context.CareerLocations.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
