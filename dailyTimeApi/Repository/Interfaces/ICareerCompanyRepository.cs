using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerCompanyRepository
{
    Task<CareerCompany?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerCompany>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<CareerCompany?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CareerCompany entity, CancellationToken cancellationToken = default);
    void Update(CareerCompany entity);
    void Remove(CareerCompany entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
