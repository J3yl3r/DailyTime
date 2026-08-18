using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerFieldRepository
{
    Task<CareerField?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerField>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CareerField entity, CancellationToken cancellationToken = default);
    void Update(CareerField entity);
    void Remove(CareerField entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
