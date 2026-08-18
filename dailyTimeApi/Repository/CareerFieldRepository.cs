using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerFieldRepository : ICareerFieldRepository
{
    private readonly AppDbContext _context;

    public CareerFieldRepository(AppDbContext context) => _context = context;

    public Task<CareerField?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerFields.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CareerField>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerFields.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerFields.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerFields.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.WorkExperiences.AsNoTracking()
            .AnyAsync(x => x.FieldId == id, cancellationToken))
            return true;
        return await _context.JobApplications.AsNoTracking()
            .AnyAsync(x => x.FieldId == id, cancellationToken);
    }

    public async Task AddAsync(CareerField entity, CancellationToken cancellationToken = default) =>
        await _context.CareerFields.AddAsync(entity, cancellationToken);

    public void Update(CareerField entity) => _context.CareerFields.Update(entity);
    public void Remove(CareerField entity) => _context.CareerFields.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
