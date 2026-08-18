using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class VaultAccountRepository : IVaultAccountRepository
{
    private readonly AppDbContext _context;

    public VaultAccountRepository(AppDbContext context) => _context = context;

    public Task<VaultAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.VaultAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VaultAccount>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await _context.VaultAccounts.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<int> CountPasswordsAsync(int accountId, CancellationToken cancellationToken = default) =>
        _context.VaultPasswords.AsNoTracking()
            .CountAsync(x => x.AccountId == accountId, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.VaultAccounts.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(VaultAccount entity, CancellationToken cancellationToken = default) =>
        await _context.VaultAccounts.AddAsync(entity, cancellationToken);

    public void Update(VaultAccount entity) => _context.VaultAccounts.Update(entity);

    public void Remove(VaultAccount entity)
    {
        _context.VaultAccounts.Attach(entity);
        _context.VaultAccounts.Remove(entity);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
