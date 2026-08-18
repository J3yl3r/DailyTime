using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface ICareerProfileRepository
{
    Task<CareerProfile?> GetAsync(CancellationToken cancellationToken = default);
    Task<CareerProfile?> GetTrackedAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CareerProfile entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
