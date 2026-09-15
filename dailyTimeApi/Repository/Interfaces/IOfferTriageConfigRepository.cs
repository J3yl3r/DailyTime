using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

public interface IOfferTriageConfigRepository
{
    /// <summary>Única fila de reglas (tracked), o null si aún no existe.</summary>
    Task<OfferTriageConfig?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(OfferTriageConfig entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
