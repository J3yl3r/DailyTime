using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerTechnologyRepository : ICareerTechnologyRepository
{
    private readonly AppDbContext _context;

    public CareerTechnologyRepository(AppDbContext context) => _context = context;

    public Task<CareerTechnology?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerTechnologies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CareerTechnology>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerTechnologies.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerTechnologies.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerTechnologies.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> AllExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0)
            return true;
        var count = await _context.CareerTechnologies.AsNoTracking()
            .CountAsync(x => distinct.Contains(x.Id), cancellationToken);
        return count == distinct.Count;
    }

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default) =>
        _context.WorkExperienceTechnologies.AsNoTracking()
            .AnyAsync(x => x.TechnologyId == id, cancellationToken);

    public async Task AddAsync(CareerTechnology entity, CancellationToken cancellationToken = default) =>
        await _context.CareerTechnologies.AddAsync(entity, cancellationToken);

    public void Update(CareerTechnology entity) => _context.CareerTechnologies.Update(entity);
    public void Remove(CareerTechnology entity) => _context.CareerTechnologies.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
