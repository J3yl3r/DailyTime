using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IVaultAccountRepository
{
    Task<VaultAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CountPasswordsAsync(int accountId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(VaultAccount entity, CancellationToken cancellationToken = default);
    void Update(VaultAccount entity);
    void Remove(VaultAccount entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
