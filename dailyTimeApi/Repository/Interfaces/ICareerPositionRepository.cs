using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerPositionRepository
{
    Task<CareerPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerPosition>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<CareerPosition?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CareerPosition entity, CancellationToken cancellationToken = default);
    void Update(CareerPosition entity);
    void Remove(CareerPosition entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
