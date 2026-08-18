using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerApplicationStatusRepository
{
    Task<CareerApplicationStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerApplicationStatus>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CareerApplicationStatus entity, CancellationToken cancellationToken = default);
    void Update(CareerApplicationStatus entity);
    void Remove(CareerApplicationStatus entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
