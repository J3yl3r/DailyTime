using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;

namespace dailyTimeApi.Repository.Interfaces;

public interface IJobOfferRepository
{
    Task<JobOffer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<JobOffer?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobOffer>> GetAllAsync(
        JobOfferFilterRequest? filter = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctCountriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctLanguagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctWorkModalitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctContractTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctTechStacksAsync(CancellationToken cancellationToken = default);
    Task<JobOffer?> FindByPortalAndKeyAsync(
        int portalId, string externalKey, CancellationToken cancellationToken = default);
    Task<JobOffer?> FindByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<JobOffer?> FindByPortalTitleCompanyAsync(
        int portalId, string title, string? company, CancellationToken cancellationToken = default);
    Task<int> BulkUpdateStatusAsync(
        IReadOnlyList<int> ids, string status, CancellationToken cancellationToken = default);
    Task<int> BulkDeleteAsync(
        IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
    /// <summary>Fija las ofertas indicadas en ese orden (arriba del puntaje).</summary>
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
    Task<int> GetMaxPinnedSortOrderAsync(CancellationToken cancellationToken = default);
    /// <summary>Ofertas activas (new/seen) A o B sin intento de análisis con IA, mejor puntaje primero (tracked).</summary>
    Task<IReadOnlyList<JobOffer>> GetPendingAiAnalysisAsync(int take, CancellationToken cancellationToken = default);
    Task<int> CountPendingAiAnalysisAsync(CancellationToken cancellationToken = default);
    Task<int> CountAiAnalyzedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
    /// <summary>Todas las ofertas con portal, para recalcular puntaje y descarte.</summary>
    Task<IReadOnlyList<JobOffer>> GetAllForTriageAsync(bool tracked, CancellationToken cancellationToken = default);
    /// <summary>Primera oferta de otro portal con el mismo título y empresa.</summary>
    Task<JobOffer?> FindCrossPortalDuplicateAsync(
        string title, string? company, int portalId, int excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(JobOffer entity, CancellationToken cancellationToken = default);
    void Update(JobOffer entity);
    void Remove(JobOffer entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
