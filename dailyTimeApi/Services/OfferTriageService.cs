using System.Text.Json;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Services;

public class OfferTriageService : IOfferTriageService
{
    private const int MaxListItems = 100;
    private const int MaxItemLength = 80;
    private const int MaxSamples = 15;

    private static readonly JsonSerializerOptions SettingsJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IJobOfferRepository _offers;
    private readonly IOfferTriageConfigRepository _config;
    private readonly ICareerProfileRepository _profiles;
    private readonly IWorkExperienceRepository _experiences;

    public OfferTriageService(
        IJobOfferRepository offers,
        IOfferTriageConfigRepository config,
        ICareerProfileRepository profiles,
        IWorkExperienceRepository experiences)
    {
        _offers = offers;
        _config = config;
        _profiles = profiles;
        _experiences = experiences;
    }

    public enum TriageChange { None, Discarded, Restored, ProtectedByUser }

    public Task<OfferTriageSettings> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        LoadSettingsAsync(cancellationToken);

    public async Task<UpdateOfferTriageResponse> UpdateSettingsAsync(
        OfferTriageSettings settings, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSettings(settings);
        var entity = await _config.GetAsync(cancellationToken);
        if (entity is null)
        {
            entity = new OfferTriageConfig();
            await _config.AddAsync(entity, cancellationToken);
        }

        entity.SettingsJson = JsonSerializer.Serialize(normalized);
        entity.UpdatedAt = DateTime.UtcNow;
        await _config.SaveChangesAsync(cancellationToken);

        var result = await RescoreAsync(normalized, dryRun: false, cancellationToken);
        return new UpdateOfferTriageResponse { Settings = normalized, Result = result };
    }

    public async Task<RescoreJobOffersResponse> PreviewAsync(
        OfferTriageSettings? settings, CancellationToken cancellationToken = default)
    {
        var effective = settings is null
            ? await LoadSettingsAsync(cancellationToken)
            : NormalizeSettings(settings);
        return await RescoreAsync(effective, dryRun: true, cancellationToken);
    }

    public async Task<RescoreJobOffersResponse> RescoreAllAsync(CancellationToken cancellationToken = default) =>
        await RescoreAsync(await LoadSettingsAsync(cancellationToken), dryRun: false, cancellationToken);

    public async Task<int> ApplyToOffersAsync(
        IReadOnlyList<JobOffer> offers, CancellationToken cancellationToken = default)
    {
        if (offers.Count == 0)
            return 0;

        var settings = await LoadSettingsAsync(cancellationToken);
        var profile = await BuildProfileAsync(settings, cancellationToken);
        var now = DateTime.UtcNow;
        var discarded = 0;

        foreach (var offer in offers)
        {
            var evaluation = OfferScorer.Evaluate(offer, profile, settings, now);
            string? duplicateReason = null;
            if (settings.AutoDiscardEnabled && settings.DiscardDuplicates && evaluation.DiscardReason is null
                && !string.IsNullOrWhiteSpace(offer.Company))
            {
                var original = await _offers.FindCrossPortalDuplicateAsync(
                    offer.Title, offer.Company, offer.JobPortalId, offer.Id, cancellationToken);
                if (original is not null && (offer.Id == 0 || original.Id < offer.Id))
                    duplicateReason = DuplicateReason(original);
            }

            if (ApplyEvaluation(offer, evaluation, duplicateReason, now) == TriageChange.Discarded)
                discarded++;
        }

        return discarded;
    }

    /// <summary>
    /// Aplica una evaluación a la oferta: siempre actualiza el puntaje; el estado solo cambia si lo fijó el sistema.
    /// </summary>
    public static TriageChange ApplyEvaluation(
        JobOffer offer, OfferEvaluation evaluation, string? duplicateReason, DateTime nowUtc)
    {
        offer.PriorityScore = evaluation.Score;
        offer.PriorityTier = evaluation.Tier;
        offer.ScoreBreakdown = JsonSerializer.Serialize(evaluation.Factors);
        offer.ScoredAt = nowUtc;

        var reason = evaluation.DiscardReason ?? duplicateReason;
        if (offer.StatusSource == JobOfferStatusSources.User)
        {
            return reason is not null && offer.Status != "discarded"
                ? TriageChange.ProtectedByUser
                : TriageChange.None;
        }

        if (reason is not null)
        {
            if (offer.Status == "new")
            {
                offer.Status = "discarded";
                offer.DiscardReason = Truncate(reason, 500);
                offer.UpdatedAt = nowUtc;
                return TriageChange.Discarded;
            }

            if (offer.Status == "discarded")
                offer.DiscardReason = Truncate(reason, 500);
            return TriageChange.None;
        }

        if (offer.Status == "discarded")
        {
            offer.Status = "new";
            offer.DiscardReason = null;
            offer.UpdatedAt = nowUtc;
            return TriageChange.Restored;
        }

        return TriageChange.None;
    }

    public static OfferTriageSettings NormalizeSettings(OfferTriageSettings settings)
    {
        if (settings.TierAMin is < 1 or > 100)
            throw new ValidationException("El puntaje mínimo de A debe estar entre 1 y 100.");
        if (settings.TierBMin < 0 || settings.TierBMin >= settings.TierAMin)
            throw new ValidationException("El puntaje mínimo de B debe ser menor que el de A.");
        if (settings.MaxAgeDays is < 0 or > 365)
            throw new ValidationException("La antigüedad máxima debe estar entre 0 y 365 días.");

        return new OfferTriageSettings
        {
            AutoDiscardEnabled = settings.AutoDiscardEnabled,
            MaxAgeDays = settings.MaxAgeDays,
            AllowedCountries = CleanList(settings.AllowedCountries),
            DiscardOnsiteAbroad = settings.DiscardOnsiteAbroad,
            DiscardOnsiteLocal = settings.DiscardOnsiteLocal,
            DiscardResidencyAbroad = settings.DiscardResidencyAbroad,
            DiscardOutOfProfile = settings.DiscardOutOfProfile,
            DiscardDuplicates = settings.DiscardDuplicates,
            ExcludedTitleKeywords = CleanList(settings.ExcludedTitleKeywords),
            BlockedCompanies = CleanList(settings.BlockedCompanies),
            PenalizeEnglishGap = settings.PenalizeEnglishGap,
            TierAMin = settings.TierAMin,
            TierBMin = settings.TierBMin
        };
    }

    private async Task<RescoreJobOffersResponse> RescoreAsync(
        OfferTriageSettings settings, bool dryRun, CancellationToken cancellationToken)
    {
        // En vista previa las entidades no se trackean: los cambios en memoria no llegan a la BD.
        var offers = await _offers.GetAllForTriageAsync(tracked: !dryRun, cancellationToken);
        var profile = await BuildProfileAsync(settings, cancellationToken);
        var duplicates = settings.AutoDiscardEnabled && settings.DiscardDuplicates
            ? FindDuplicates(offers)
            : new Dictionary<int, string>();
        var now = DateTime.UtcNow;
        var response = new RescoreJobOffersResponse { DryRun = dryRun, Evaluated = offers.Count };
        var reasons = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var offer in offers)
        {
            var evaluation = OfferScorer.Evaluate(offer, profile, settings, now);
            var change = ApplyEvaluation(offer, evaluation, duplicates.GetValueOrDefault(offer.Id), now);
            switch (change)
            {
                case TriageChange.Discarded:
                    response.NewlyDiscarded++;
                    var bucket = ReasonBucket(offer.DiscardReason!, settings);
                    reasons[bucket] = reasons.GetValueOrDefault(bucket) + 1;
                    if (response.Samples.Count < MaxSamples)
                    {
                        response.Samples.Add(new TriageSampleResponse
                        {
                            Id = offer.Id,
                            Title = offer.Title,
                            Company = offer.Company,
                            PortalName = offer.JobPortal?.Name ?? string.Empty,
                            Reason = offer.DiscardReason!
                        });
                    }
                    break;
                case TriageChange.Restored:
                    response.Restored++;
                    break;
                case TriageChange.ProtectedByUser:
                    response.ProtectedByUser++;
                    break;
            }

            if (offer.Status == "discarded")
                continue;

            response.ActiveAfter++;
            switch (offer.PriorityTier)
            {
                case "A": response.TierA++; break;
                case "B": response.TierB++; break;
                default: response.TierC++; break;
            }
        }

        response.DiscardReasons = reasons
            .OrderByDescending(r => r.Value)
            .Select(r => new DiscardReasonCountResponse { Reason = r.Key, Count = r.Value })
            .ToList();

        if (!dryRun)
            await _offers.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static Dictionary<int, string> FindDuplicates(IReadOnlyList<JobOffer> offers)
    {
        var result = new Dictionary<int, string>();
        var groups = offers
            .Where(o => !string.IsNullOrWhiteSpace(o.Company))
            .GroupBy(o => (Title: OfferScorer.Normalize(o.Title), Company: OfferScorer.Normalize(o.Company)));

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(o => o.Id).ToList();
            if (ordered.Select(o => o.JobPortalId).Distinct().Count() < 2)
                continue;

            var original = ordered[0];
            foreach (var duplicate in ordered.Skip(1).Where(o => o.JobPortalId != original.JobPortalId))
                result[duplicate.Id] = DuplicateReason(original);
        }

        return result;
    }

    private async Task<TriageProfile> BuildProfileAsync(
        OfferTriageSettings settings, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetAsync(cancellationToken);
        var experiences = await _experiences.GetAllAsync(cancellationToken);
        return TriageProfileBuilder.Build(profile, experiences, settings, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private async Task<OfferTriageSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var entity = await _config.GetAsync(cancellationToken);
        if (entity is null || string.IsNullOrWhiteSpace(entity.SettingsJson))
            return new OfferTriageSettings();

        try
        {
            var stored = JsonSerializer.Deserialize<OfferTriageSettings>(entity.SettingsJson, SettingsJsonOptions);
            return NormalizeSettings(stored ?? new OfferTriageSettings());
        }
        catch (Exception ex) when (ex is JsonException or ValidationException)
        {
            return new OfferTriageSettings();
        }
    }

    private static string DuplicateReason(JobOffer original) =>
        $"Duplicada de #{original.Id} en {original.JobPortal?.Name ?? "otro portal"}";

    private static string ReasonBucket(string reason, OfferTriageSettings settings) =>
        reason.StartsWith("Publicada hace", StringComparison.Ordinal)
            ? $"Publicada hace más de {settings.MaxAgeDays} días"
            : reason.StartsWith("Duplicada de", StringComparison.Ordinal)
                ? "Duplicada en otro portal"
                : reason;

    private static List<string> CleanList(IEnumerable<string>? values) =>
        (values ?? Enumerable.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Where(v => v.Length <= MaxItemLength)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxListItems)
            .ToList();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
