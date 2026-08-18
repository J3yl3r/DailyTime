using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class VaultPasswordRepository : IVaultPasswordRepository
{
    private readonly AppDbContext _context;

    public VaultPasswordRepository(AppDbContext context) => _context = context;

    public Task<VaultPassword?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.VaultPasswords.AsNoTracking()
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VaultPassword>> GetByAccountIdAsync(
        int accountId, CancellationToken cancellationToken = default) =>
        await _context.VaultPasswords.AsNoTracking()
            .Include(x => x.Service)
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.Service != null ? x.Service.Name : string.Empty)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(VaultPassword entity, CancellationToken cancellationToken = default) =>
        await _context.VaultPasswords.AddAsync(entity, cancellationToken);

    public void Update(VaultPassword entity) => _context.VaultPasswords.Update(entity);

    public void Remove(VaultPassword entity)
    {
        _context.VaultPasswords.Attach(entity);
        _context.VaultPasswords.Remove(entity);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
