using System.Text.Json;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Ai;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class JobOfferService : IJobOfferService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "new", "seen", "discarded", "applied"
    };

    private readonly IJobOfferRepository _repository;
    private readonly IJobPortalRepository _portalRepository;
    private static readonly JsonSerializerOptions ScoreJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IJobApplicationService _applicationService;
    private readonly IOfferTriageService _triage;
    private readonly OfferAiState _aiState;

    public JobOfferService(
        IJobOfferRepository repository,
        IJobPortalRepository portalRepository,
        IJobApplicationService applicationService,
        IOfferTriageService triage,
        OfferAiState aiState)
    {
        _repository = repository;
        _portalRepository = portalRepository;
        _applicationService = applicationService;
        _triage = triage;
        _aiState = aiState;
    }

    public async Task<IReadOnlyList<JobOfferResponse>> GetAllAsync(
        JobOfferFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(filter, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<JobOfferMetaResponse> GetMetaAsync(CancellationToken cancellationToken = default) =>
        new()
        {
            Countries = await _repository.GetDistinctCountriesAsync(cancellationToken),
            Languages = await _repository.GetDistinctLanguagesAsync(cancellationToken),
            WorkModalities = await _repository.GetDistinctWorkModalitiesAsync(cancellationToken),
            ContractTypes = await _repository.GetDistinctContractTypesAsync(cancellationToken),
            TechStacks = await _repository.GetDistinctTechStacksAsync(cancellationToken)
        };

    public async Task<JobOfferResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Oferta {id} no encontrada."));

    public async Task<UpsertJobOffersResponse> UpsertBatchAsync(
        UpsertJobOffersRequest request, CancellationToken cancellationToken = default)
    {
        _ = await _portalRepository.GetByIdAsync(request.JobPortalId, cancellationToken)
            ?? throw new NotFoundException($"Portal {request.JobPortalId} no encontrado.");

        var inserted = 0;
        var updated = 0;
        var now = DateTime.UtcNow;
        var touched = new List<JobOffer>();
        var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchContent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Offers)
        {
            var title = (item.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var url = NormalizeUrl(item.Url);
            var externalKey = (item.ExternalKey ?? url ?? ContentKey(title, item.Company)).Trim();
            if (externalKey.Length > 450)
                externalKey = externalKey[..450];

            var contentKey = ContentKey(title, item.Company);
            if (!batchKeys.Add(externalKey) || !batchContent.Add(contentKey))
                continue;

            var existing = await _repository.FindByPortalAndKeyAsync(
                request.JobPortalId, externalKey, cancellationToken);

            existing ??= await _repository.FindByPortalTitleCompanyAsync(
                request.JobPortalId, title, item.Company, cancellationToken);

            if (existing is null)
            {
                var created = new JobOffer
                {
                    JobPortalId = request.JobPortalId,
                    Title = Truncate(title, 300),
                    Company = NormalizeOptional(item.Company, 200),
                    Location = NormalizeOptional(item.Location, 200),
                    Url = NormalizeOptional(url, 1000),
                    ExternalKey = externalKey,
                    DescriptionSnippet = NormalizeOptional(item.DescriptionSnippet, 2000)
                        ?? TruncateForSnippet(item.Description),
                    Description = NormalizeDescription(item.Description)
                        ?? NormalizeDescription(item.DescriptionSnippet),
                    Country = NormalizeOptional(item.Country, 80),
                    Language = NormalizeOptional(item.Language, 20),
                    WorkModality = NormalizeOptional(item.WorkModality, 80),
                    ContractType = NormalizeOptional(item.ContractType, 80),
                    TechStack = NormalizeOptional(item.TechStack, 200)
                        ?? DetectTechStackFallback(item),
                    PostedAt = item.PostedAt,
                    Status = "new",
                    CapturedAt = now,
                    UpdatedAt = now
                };
                await _repository.AddAsync(created, cancellationToken);
                touched.Add(created);
                inserted++;
            }
            else
            {
                existing.Title = Truncate(title, 300);
                existing.Company = NormalizeOptional(item.Company, 200);
                existing.Location = NormalizeOptional(item.Location, 200);
                existing.Url = NormalizeOptional(url, 1000) ?? existing.Url;
                if (!string.Equals(existing.ExternalKey, externalKey, StringComparison.Ordinal))
                    existing.ExternalKey = externalKey;
                var incomingDescription = NormalizeDescription(item.Description)
                    ?? NormalizeDescription(item.DescriptionSnippet);
                // Preferir descripción más completa al re-scrapear (evita quedarse con solo “Requerimientos”).
                existing.Description = PreferRicherDescription(incomingDescription, existing.Description);
                existing.DescriptionSnippet = NormalizeOptional(item.DescriptionSnippet, 2000)
                    ?? TruncateForSnippet(existing.Description)
                    ?? existing.DescriptionSnippet;
                existing.Country = NormalizeOptional(item.Country, 80) ?? existing.Country;
                existing.Language = NormalizeOptional(item.Language, 20) ?? existing.Language;
                existing.WorkModality = NormalizeOptional(item.WorkModality, 80) ?? existing.WorkModality;
                existing.ContractType = NormalizeOptional(item.ContractType, 80) ?? existing.ContractType;
                existing.TechStack = NormalizeOptional(item.TechStack, 200)
                    ?? DetectTechStackFallback(item)
                    ?? existing.TechStack;
                existing.PostedAt = item.PostedAt ?? existing.PostedAt;
                existing.UpdatedAt = now;
                _repository.Update(existing);
                touched.Add(existing);
                updated++;
            }
        }

        // Puntaje y descarte automático antes de guardar; nunca pisa un estado decidido por el usuario.
        var autoDiscarded = await _triage.ApplyToOffersAsync(touched, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
        // Las nuevas activas se analizan con IA en segundo plano (no retrasa la respuesta al worker).
        _aiState.RequestRun();
        return new UpsertJobOffersResponse
        {
            Inserted = inserted,
            Updated = updated,
            Total = inserted + updated,
            AutoDiscarded = autoDiscarded
        };
    }

    public async Task<JobOfferResponse> UpdateStatusAsync(
        int id, UpdateJobOfferStatusRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Oferta {id} no encontrada.");

        var status = NormalizeStatus(request.Status);
        entity.Status = status;
        entity.StatusSource = JobOfferStatusSources.User;
        entity.DiscardReason = null;
        entity.UpdatedAt = DateTime.UtcNow;

        if (status == "applied")
            await _applicationService.EnsureFromOfferAsync(entity, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var fresh = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Oferta {id} no encontrada.");
        return Map(fresh);
    }

    public async Task<BulkJobOfferActionResponse> BulkUpdateStatusAsync(
        BulkJobOfferStatusRequest request, CancellationToken cancellationToken = default)
    {
        var status = NormalizeStatus(request.Status);
        var ids = request.Ids?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            throw new ValidationException("Debes indicar al menos un id.");

        if (status == "applied")
        {
            var affected = 0;
            foreach (var id in ids)
            {
                var entity = await _repository.GetTrackedByIdAsync(id, cancellationToken);
                if (entity is null)
                    continue;

                entity.Status = status;
                entity.StatusSource = JobOfferStatusSources.User;
                entity.DiscardReason = null;
                entity.UpdatedAt = DateTime.UtcNow;
                await _applicationService.EnsureFromOfferAsync(entity, cancellationToken);
                affected++;
            }

            await _repository.SaveChangesAsync(cancellationToken);
            return new BulkJobOfferActionResponse { Affected = affected };
        }

        var updated = await _repository.BulkUpdateStatusAsync(ids, status, cancellationToken);
        return new BulkJobOfferActionResponse { Affected = updated };
    }

    public async Task<BulkJobOfferActionResponse> BulkDeleteAsync(
        BulkJobOfferDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            throw new ValidationException("Debes indicar al menos un id.");

        var affected = await _repository.BulkDeleteAsync(ids, cancellationToken);
        return new BulkJobOfferActionResponse { Affected = affected };
    }

    public async Task ReorderAsync(
        ReorderJobOffersRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return;

        await _repository.ReorderAsync(ids, cancellationToken);
    }

    public async Task<JobOfferResponse> SetPinnedAsync(
        int id, bool pinned, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Oferta {id} no encontrada.");

        if (entity.IsPinned != pinned)
        {
            entity.SortOrder = pinned
                ? await _repository.GetMaxPinnedSortOrderAsync(cancellationToken) + 1
                : 0;
            entity.IsPinned = pinned;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Oferta {id} no encontrada.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeStatus(string? raw)
    {
        var status = (raw ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(status))
            throw new ValidationException("Status inválido. Usa: new, seen, discarded, applied.");
        return status;
    }

    private static JobOfferResponse Map(JobOffer entity) => new()
    {
        Id = entity.Id,
        JobPortalId = entity.JobPortalId,
        PortalName = entity.JobPortal?.Name ?? string.Empty,
        Title = entity.Title,
        Company = entity.Company,
        Location = entity.Location,
        Url = entity.Url,
        ExternalKey = entity.ExternalKey,
        DescriptionSnippet = entity.DescriptionSnippet,
        Description = entity.Description,
        Country = entity.Country,
        Language = entity.Language,
        WorkModality = entity.WorkModality,
        ContractType = entity.ContractType,
        TechStack = entity.TechStack,
        PostedAt = entity.PostedAt,
        Status = entity.Status,
        StatusSource = entity.StatusSource,
        DiscardReason = entity.DiscardReason,
        PriorityScore = entity.PriorityScore,
        PriorityTier = entity.PriorityTier,
        ScoreFactors = ReadScoreFactors(entity.ScoreBreakdown),
        ScoredAt = entity.ScoredAt,
        AiAnalysis = OfferAiJson.TryRead(entity.AiAnalysis),
        AiAnalyzedAt = entity.AiAnalyzedAt,
        AiModel = entity.AiModel,
        AiError = entity.AiError,
        IsPinned = entity.IsPinned,
        SortOrder = entity.SortOrder,
        CapturedAt = entity.CapturedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static IReadOnlyList<ScoreFactorResponse> ReadScoreFactors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<ScoreFactorResponse>>(json, ScoreJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? DetectTechStackFallback(UpsertJobOfferItem item)
    {
        var text = string.Join(" ",
            new[] { item.Title, item.DescriptionSnippet, item.Description }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var t = text.ToLowerInvariant();
        var stacks = new List<string>();
        if (t.Contains(".net") || t.Contains("dotnet") || t.Contains("c#") || t.Contains("asp.net"))
            stacks.Add(".NET");
        if (t.Contains("next.js") || t.Contains("nextjs") || t.Contains("next js"))
            stacks.Add("Next.js");
        if (t.Contains("react"))
            stacks.Add("React");
        if (t.Contains("javascript") || t.Contains("typescript") || t.Contains("node.js"))
            stacks.Add("JavaScript");
        if (System.Text.RegularExpressions.Regex.IsMatch(t, @"\bjava\b") && !t.Contains("javascript"))
            stacks.Add("Java");
        if (t.Contains("python"))
            stacks.Add("Python");
        if (t.Contains("angular"))
            stacks.Add("Angular");
        if (System.Text.RegularExpressions.Regex.IsMatch(t, @"\bvue\b") || t.Contains("vue.js"))
            stacks.Add("Vue");
        return stacks.Count == 0 ? null : string.Join(", ", stacks);
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? NormalizeDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        const int max = 50_000;
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? PreferRicherDescription(string? incoming, string? existing)
    {
        if (string.IsNullOrWhiteSpace(incoming))
            return existing;
        if (string.IsNullOrWhiteSpace(existing))
            return incoming;
        if (incoming.Length >= existing.Length)
            return incoming;
        // Incoming más corto solo gana si el existente parece basura de requerimientos.
        var ex = existing.Trim();
        if (ex.Length < 200
            && (ex.StartsWith("Requerimientos", StringComparison.OrdinalIgnoreCase)
                || ex.Contains("Educación mínima", StringComparison.OrdinalIgnoreCase)))
            return incoming;
        return existing;
    }

    private static string? TruncateForSnippet(string? value) =>
        NormalizeOptional(value, 2000);

    private static string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            return NormalizeOptional(value, 1000);

        var host = uri.Host.ToLowerInvariant();

        // Indeed: /pagead/clk without ?jk= returns 404. Prefer /viewjob?jk=...
        if (IsIndeedHost(host))
        {
            var jk = GetQueryParam(uri, "jk");
            if (!string.IsNullOrWhiteSpace(jk))
            {
                return NormalizeOptional(
                    $"https://{host}/viewjob?jk={Uri.EscapeDataString(jk)}",
                    1000);
            }

            if (uri.AbsolutePath.Contains("pagead/clk", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(uri.Query))
                return null;

            var indeedBuilder = new UriBuilder(uri)
            {
                Scheme = uri.Scheme.ToLowerInvariant(),
                Host = host,
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                Fragment = string.Empty
            };
            var indeedPath = indeedBuilder.Path.TrimEnd('/');
            indeedBuilder.Path = string.IsNullOrEmpty(indeedPath) ? "/" : indeedPath;
            return NormalizeOptional(indeedBuilder.Uri.ToString(), 1000);
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = host,
            Port = uri.IsDefaultPort ? -1 : uri.Port,
            Query = string.Empty,
            Fragment = string.Empty
        };
        var path = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrEmpty(path) ? "/" : path;
        return NormalizeOptional(builder.Uri.ToString(), 1000);
    }

    private static bool IsIndeedHost(string host) =>
        host.Equals("indeed.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".indeed.com", StringComparison.OrdinalIgnoreCase);

    private static string? GetQueryParam(Uri uri, string key)
    {
        var query = uri.Query;
        if (string.IsNullOrEmpty(query))
            return null;

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 0)
                continue;
            if (!string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
                continue;
            return pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
        }

        return null;
    }

    private static string ContentKey(string title, string? company) =>
        $"{title.Trim().ToLowerInvariant()}|{(company ?? string.Empty).Trim().ToLowerInvariant()}";

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
