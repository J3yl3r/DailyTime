using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _context;

    public CompanyRepository(AppDbContext context) => _context = context;

    public Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Company>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Companies.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Companies.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _context.TaskItems.AsNoTracking()
            .AnyAsync(x => x.CompanyId == id, cancellationToken))
            return true;

        return await _context.Notes.AsNoTracking()
            .AnyAsync(x => x.CompanyId == id, cancellationToken);
    }

    public async Task AddAsync(Company entity, CancellationToken cancellationToken = default) =>
        await _context.Companies.AddAsync(entity, cancellationToken);

    public void Update(Company entity) => _context.Companies.Update(entity);
    public void Remove(Company entity) => _context.Companies.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
