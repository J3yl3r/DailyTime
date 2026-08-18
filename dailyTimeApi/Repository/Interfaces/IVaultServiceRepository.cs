using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IVaultServiceRepository
{
    Task<VaultService?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultService>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountPasswordsAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(VaultService entity, CancellationToken cancellationToken = default);
    void Update(VaultService entity);
    void Remove(VaultService entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
