using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Company>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Company entity, CancellationToken cancellationToken = default);
    void Update(Company entity);
    void Remove(Company entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
