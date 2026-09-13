using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IScrapeScheduleRepository
{
    /// <summary>Única fila del horario (tracked), o null si aún no existe.</summary>
    Task<ScrapeSchedule?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ScrapeSchedule entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
