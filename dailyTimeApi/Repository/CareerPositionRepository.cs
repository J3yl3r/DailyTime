using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerPositionRepository : ICareerPositionRepository
{
    private readonly AppDbContext _context;

    public CareerPositionRepository(AppDbContext context) => _context = context;

    public Task<CareerPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerPositions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CareerPosition>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerPositions.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerPositions.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<CareerPosition?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return _context.CareerPositions
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerPositions.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.WorkExperiences.AsNoTracking()
            .AnyAsync(x => x.PositionId == id, cancellationToken))
            return true;
        return await _context.JobApplications.AsNoTracking()
            .AnyAsync(x => x.PositionId == id, cancellationToken);
    }

    public async Task AddAsync(CareerPosition entity, CancellationToken cancellationToken = default) =>
        await _context.CareerPositions.AddAsync(entity, cancellationToken);

    public void Update(CareerPosition entity) => _context.CareerPositions.Update(entity);
    public void Remove(CareerPosition entity) => _context.CareerPositions.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
