using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerLocationRepository
{
    Task<CareerLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerLocation>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<CareerLocation?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CareerLocation entity, CancellationToken cancellationToken = default);
    void Update(CareerLocation entity);
    void Remove(CareerLocation entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
