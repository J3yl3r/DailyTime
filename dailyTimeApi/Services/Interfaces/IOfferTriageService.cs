using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Services.Interfaces;

public interface IOfferTriageService
{
    Task<OfferTriageSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reglas guardadas y perfil resuelto (lo usa el análisis con IA).</summary>
    Task<(OfferTriageSettings Settings, TriageProfile Profile)> GetContextAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Guarda las reglas y recalcula todas las ofertas.</summary>
    Task<UpdateOfferTriageResponse> UpdateSettingsAsync(
        OfferTriageSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Simula el recálculo con las reglas indicadas (o las guardadas) sin guardar nada.</summary>
    Task<RescoreJobOffersResponse> PreviewAsync(
        OfferTriageSettings? settings, CancellationToken cancellationToken = default);

    /// <summary>Recalcula todas las ofertas con las reglas guardadas (p. ej. tras cambiar el perfil).</summary>
    Task<RescoreJobOffersResponse> RescoreAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evalúa ofertas recién capturadas o actualizadas (entidades trackeadas, sin guardar).
    /// Devuelve cuántas quedaron descartadas.
    /// </summary>
    Task<int> ApplyToOffersAsync(IReadOnlyList<JobOffer> offers, CancellationToken cancellationToken = default);
}
