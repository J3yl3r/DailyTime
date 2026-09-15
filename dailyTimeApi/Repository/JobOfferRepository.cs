using dailyTimeApi.Data;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Repository;

public class JobOfferRepository : IJobOfferRepository
{
    private readonly AppDbContext _context;

    public JobOfferRepository(AppDbContext context) => _context = context;

    public Task<JobOffer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<JobOffer?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.JobOffers
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JobOffer>> GetAllAsync(
        JobOfferFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.JobOffers.AsNoTracking().Include(x => x.JobPortal), filter);
        // Fijadas primero (en su orden manual), luego por puntaje de prioridad y fecha.
        return await query
            .OrderByDescending(x => x.IsPinned)
            .ThenBy(x => x.IsPinned ? x.SortOrder : 0)
            .ThenByDescending(x => x.PriorityScore ?? -1)
            .ThenByDescending(x => x.PostedAt ?? x.CapturedAt)
            .ThenByDescending(x => x.CapturedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctCountriesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.Country != null && x.Country != "")
            .Select(x => x.Country!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctLanguagesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.Language != null && x.Language != "")
            .Select(x => x.Language!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctWorkModalitiesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.WorkModality != null && x.WorkModality != "")
            .Select(x => x.WorkModality!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctContractTypesAsync(
        CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AsNoTracking()
            .Where(x => x.ContractType != null && x.ContractType != "")
            .Select(x => x.ContractType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctTechStacksAsync(
        CancellationToken cancellationToken = default)
    {
        var raw = await _context.JobOffers.AsNoTracking()
            .Where(x => x.TechStack != null && x.TechStack != "")
            .Select(x => x.TechStack!)
            .ToListAsync(cancellationToken);

        return raw
            .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<JobOffer?> FindByPortalAndKeyAsync(
        int portalId, string externalKey, CancellationToken cancellationToken = default) =>
        _context.JobOffers.FirstOrDefaultAsync(
            x => x.JobPortalId == portalId && x.ExternalKey == externalKey,
            cancellationToken);

    public async Task<JobOffer?> FindByUrlAsync(
        string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var normalized = url.Trim();
        var byUrl = await _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(x => x.Url == normalized, cancellationToken);
        if (byUrl != null)
            return byUrl;

        return await _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .FirstOrDefaultAsync(
                x => x.ExternalKey == normalized ||
                     (x.Url != null && x.Url.StartsWith(normalized)),
                cancellationToken);
    }

    public Task<JobOffer?> FindByPortalTitleCompanyAsync(
        int portalId, string title, string? company, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();
        var normalizedCompany = (company ?? string.Empty).Trim().ToLower();
        return _context.JobOffers.FirstOrDefaultAsync(
            x => x.JobPortalId == portalId
                 && x.Title.ToLower() == normalizedTitle
                 && ((x.Company ?? string.Empty).ToLower() == normalizedCompany),
            cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(
        IReadOnlyList<int> ids, string status, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        return await _context.JobOffers
            .Where(x => ids.Contains(x.Id))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(x => x.Status, status)
                    .SetProperty(x => x.StatusSource, JobOfferStatusSources.User)
                    .SetProperty(x => x.DiscardReason, (string?)null)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public async Task<int> BulkDeleteAsync(
        IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        return await _context.JobOffers
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ReorderAsync(
        IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        if (orderedIds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var id = orderedIds[i];
            var order = i;
            await _context.JobOffers
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(x => x.IsPinned, true)
                        .SetProperty(x => x.SortOrder, order)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
        }
    }

    public async Task<int> GetMaxPinnedSortOrderAsync(CancellationToken cancellationToken = default) =>
        await _context.JobOffers
            .Where(x => x.IsPinned)
            .MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? -1;

    public async Task<IReadOnlyList<JobOffer>> GetPendingAiAnalysisAsync(
        int take, CancellationToken cancellationToken = default) =>
        await PendingAiAnalysis()
            .Include(x => x.JobPortal)
            .OrderByDescending(x => x.PriorityScore)
            .ThenByDescending(x => x.PostedAt ?? x.CapturedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountPendingAiAnalysisAsync(CancellationToken cancellationToken = default) =>
        PendingAiAnalysis().CountAsync(cancellationToken);

    public Task<int> CountAiAnalyzedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        _context.JobOffers.CountAsync(x => x.AiAnalyzedAt != null && x.AiAnalyzedAt >= sinceUtc, cancellationToken);

    private IQueryable<JobOffer> PendingAiAnalysis() =>
        _context.JobOffers.Where(x =>
            x.AiAnalyzedAt == null
            && (x.Status == "new" || x.Status == "seen")
            && (x.PriorityTier == "A" || x.PriorityTier == "B"));

    public async Task<IReadOnlyList<JobOffer>> GetAllForTriageAsync(
        bool tracked, CancellationToken cancellationToken = default)
    {
        var query = _context.JobOffers.Include(x => x.JobPortal).AsQueryable();
        if (!tracked)
            query = query.AsNoTracking();
        return await query.OrderBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public Task<JobOffer?> FindCrossPortalDuplicateAsync(
        string title, string? company, int portalId, int excludeId, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim().ToLower();
        var normalizedCompany = (company ?? string.Empty).Trim().ToLower();
        return _context.JobOffers.AsNoTracking()
            .Include(x => x.JobPortal)
            .Where(x => x.JobPortalId != portalId
                        && x.Id != excludeId
                        && x.Title.ToLower() == normalizedTitle
                        && (x.Company ?? string.Empty).ToLower() == normalizedCompany)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(JobOffer entity, CancellationToken cancellationToken = default) =>
        await _context.JobOffers.AddAsync(entity, cancellationToken);

    public void Update(JobOffer entity) => _context.JobOffers.Update(entity);

    public void Remove(JobOffer entity) => _context.JobOffers.Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static IQueryable<JobOffer> ApplyFilter(
        IQueryable<JobOffer> query, JobOfferFilterRequest? filter)
    {
        if (filter is null)
            return query;

        var portalIds = filter.PortalIds?.Where(p => p > 0).ToList() ?? [];
        if (filter.PortalId.HasValue && filter.PortalId.Value > 0 && !portalIds.Contains(filter.PortalId.Value))
            portalIds.Add(filter.PortalId.Value);
        if (portalIds.Count > 0)
            query = query.Where(x => portalIds.Contains(x.JobPortalId));

        var statuses = filter.Statuses?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToLower()).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var single = filter.Status.Trim().ToLower();
            if (!statuses.Contains(single))
                statuses.Add(single);
        }
        if (statuses.Count > 0)
            query = query.Where(x => statuses.Contains(x.Status.ToLower()));

        var tiers = filter.Tiers?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .ToList() ?? [];
        if (tiers.Count > 0)
        {
            var includeUnscored = tiers.Remove("NONE");
            query = query.Where(x =>
                (x.PriorityTier != null && tiers.Contains(x.PriorityTier))
                || (includeUnscored && x.PriorityTier == null));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Title, term)
                || (x.Company != null && EF.Functions.Like(x.Company, term))
                || (x.Location != null && EF.Functions.Like(x.Location, term)));
        }

        var countries = filter.Countries?.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(filter.Country) && !countries.Contains(filter.Country.Trim()))
            countries.Add(filter.Country.Trim());
        if (countries.Count > 0)
            query = query.Where(x => x.Country != null && countries.Contains(x.Country));

        var languages = filter.Languages?.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(filter.Language) && !languages.Contains(filter.Language.Trim()))
            languages.Add(filter.Language.Trim());
        if (languages.Count > 0)
            query = query.Where(x => x.Language != null && languages.Contains(x.Language));

        var modalities = filter.WorkModalities?.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(filter.WorkModality) && !modalities.Contains(filter.WorkModality.Trim()))
            modalities.Add(filter.WorkModality.Trim());
        if (modalities.Count > 0)
            query = query.Where(x => x.WorkModality != null && modalities.Contains(x.WorkModality));

        var contractTypes = filter.ContractTypes?.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(filter.ContractType) && !contractTypes.Contains(filter.ContractType.Trim()))
            contractTypes.Add(filter.ContractType.Trim());
        if (contractTypes.Count > 0)
            query = query.Where(x => x.ContractType != null && contractTypes.Contains(x.ContractType));

        var techStacks = filter.TechStacks?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(filter.TechStack) && !techStacks.Contains(filter.TechStack.Trim()))
            techStacks.Add(filter.TechStack.Trim());
        if (techStacks.Count > 0)
        {
            var predicate = PredicateBuilderOrTechStacks(techStacks);
            query = query.Where(predicate);
        }

        if (filter.CapturedFrom.HasValue)
            query = query.Where(x => x.CapturedAt >= filter.CapturedFrom.Value);

        if (filter.CapturedTo.HasValue)
        {
            var capturedTo = filter.CapturedTo.Value;
            if (capturedTo.TimeOfDay == TimeSpan.Zero)
                capturedTo = capturedTo.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CapturedAt <= capturedTo);
        }

        if (filter.PostedFrom.HasValue)
            query = query.Where(x => x.PostedAt != null && x.PostedAt >= filter.PostedFrom.Value);

        if (filter.PostedTo.HasValue)
        {
            var postedTo = filter.PostedTo.Value;
            if (postedTo.TimeOfDay == TimeSpan.Zero)
                postedTo = postedTo.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.PostedAt != null && x.PostedAt <= postedTo);
        }

        return query;
    }

    private static System.Linq.Expressions.Expression<Func<JobOffer, bool>> PredicateBuilderOrTechStacks(List<string> techStacks)
    {
        var param = System.Linq.Expressions.Expression.Parameter(typeof(JobOffer), "x");
        var prop = System.Linq.Expressions.Expression.Property(param, nameof(JobOffer.TechStack));
        var notNull = System.Linq.Expressions.Expression.NotEqual(prop, System.Linq.Expressions.Expression.Constant(null, typeof(string)));

        var efFunctions = System.Linq.Expressions.Expression.Property(null, typeof(EF), nameof(EF.Functions));
        var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like),
            new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

        System.Linq.Expressions.Expression? combinedLikes = null;
        foreach (var tech in techStacks)
        {
            var pattern = System.Linq.Expressions.Expression.Constant($"%{tech}%", typeof(string));
            var likeCall = System.Linq.Expressions.Expression.Call(null, likeMethod, efFunctions, prop, pattern);
            combinedLikes = combinedLikes == null ? likeCall : System.Linq.Expressions.Expression.OrElse(combinedLikes, likeCall);
        }

        var body = combinedLikes != null
            ? System.Linq.Expressions.Expression.AndAlso(notNull, combinedLikes)
            : notNull;

        return System.Linq.Expressions.Expression.Lambda<Func<JobOffer, bool>>(body, param);
    }
}
