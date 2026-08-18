using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CareerCompanyRepository : ICareerCompanyRepository
{
    private readonly AppDbContext _context;

    public CareerCompanyRepository(AppDbContext context) => _context = context;

    public Task<CareerCompany?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CareerCompany>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerCompanies.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CareerCompanies.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<CareerCompany?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return _context.CareerCompanies
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.CareerCompanies.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.WorkExperiences.AsNoTracking()
            .AnyAsync(x => x.CompanyId == id, cancellationToken))
            return true;
        return await _context.JobApplications.AsNoTracking()
            .AnyAsync(x => x.CompanyId == id, cancellationToken);
    }

    public async Task AddAsync(CareerCompany entity, CancellationToken cancellationToken = default) =>
        await _context.CareerCompanies.AddAsync(entity, cancellationToken);

    public void Update(CareerCompany entity) => _context.CareerCompanies.Update(entity);
    public void Remove(CareerCompany entity) => _context.CareerCompanies.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
