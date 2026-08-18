using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IVaultPasswordRepository
{
    Task<VaultPassword?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultPassword>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task AddAsync(VaultPassword entity, CancellationToken cancellationToken = default);
    void Update(VaultPassword entity);
    void Remove(VaultPassword entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
