using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class VaultServiceRepository : IVaultServiceRepository
{
    private readonly AppDbContext _context;

    public VaultServiceRepository(AppDbContext context) => _context = context;

    public Task<VaultService?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.VaultServices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VaultService>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.VaultServices.AsNoTracking().AsQueryable();
        if (onlyActive == true)
            query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.VaultServices.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default) =>
        _context.VaultPasswords.AsNoTracking().AnyAsync(x => x.ServiceId == id, cancellationToken);

    public Task<int> CountPasswordsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.VaultPasswords.AsNoTracking().CountAsync(x => x.ServiceId == id, cancellationToken);

    public async Task AddAsync(VaultService entity, CancellationToken cancellationToken = default) =>
        await _context.VaultServices.AddAsync(entity, cancellationToken);

    public void Update(VaultService entity) => _context.VaultServices.Update(entity);
    public void Remove(VaultService entity) => _context.VaultServices.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
