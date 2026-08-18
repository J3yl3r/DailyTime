using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerApplicationStatusRepository : ICareerApplicationStatusRepository
{
    private readonly AppDbContext _context;

    public CareerApplicationStatusRepository(AppDbContext context) => _context = context;

    public Task<CareerApplicationStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerApplicationStatuses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CareerApplicationStatus>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerApplicationStatuses.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerApplicationStatuses.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerApplicationStatuses.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobApplications.AsNoTracking().AnyAsync(x => x.StatusId == id, cancellationToken);

    public async Task AddAsync(CareerApplicationStatus entity, CancellationToken cancellationToken = default) =>
        await _context.CareerApplicationStatuses.AddAsync(entity, cancellationToken);

    public void Update(CareerApplicationStatus entity) => _context.CareerApplicationStatuses.Update(entity);
    public void Remove(CareerApplicationStatus entity) => _context.CareerApplicationStatuses.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
