using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerTechnologyRepository
{
    Task<CareerTechnology?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerTechnology>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AllExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CareerTechnology entity, CancellationToken cancellationToken = default);
    void Update(CareerTechnology entity);
    void Remove(CareerTechnology entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
