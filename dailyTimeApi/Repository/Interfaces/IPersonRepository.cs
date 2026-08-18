using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Person entity, CancellationToken cancellationToken = default);
    void Update(Person entity);
    void Remove(Person entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
