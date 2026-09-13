using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class JobPortalService : IJobPortalService
{
    private readonly IJobPortalRepository _repository;
    private readonly IJobPortalScrapeLogRepository _scrapeLogRepository;

    public JobPortalService(
        IJobPortalRepository repository,
        IJobPortalScrapeLogRepository scrapeLogRepository)
    {
        _repository = repository;
        _scrapeLogRepository = scrapeLogRepository;
    }

    public async Task<IReadOnlyList<JobPortalResponse>> GetAllAsync(
        bool? onlyActive = null,
        bool queuedOnly = false,
        bool autoOnly = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(onlyActive, queuedOnly, autoOnly, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<JobPortalResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Portal {id} no encontrado."));

    public async Task<JobPortalResponse> SetAutoScrapeAsync(
        int id, bool enabled, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Portal {id} no encontrado.");
        if (enabled && !entity.IsActive)
            throw new ValidationException("Activa el portal antes de incluirlo en la ejecución automática.");

        entity.AutoScrapeEnabled = enabled;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<JobPortalResponse> CreateAsync(
        CreateJobPortalRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        if (await _repository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ValidationException($"Ya existe el portal '{name}'.");

        var now = DateTime.UtcNow;
        var entity = new JobPortal
        {
            Name = name,
            Url = NormalizeUrl(request.Url),
            LoginUrl = NormalizeOptional(request.LoginUrl, 500),
            Notes = NormalizeOptional(request.Notes, 2000),
            ScrapeConfig = NormalizeOptional(request.ScrapeConfig, 4000),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<JobPortalResponse> UpdateAsync(
        int id, UpdateJobPortalRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Portal {id} no encontrado.");
        var name = NormalizeName(request.Name);
        if (await _repository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ValidationException($"Ya existe el portal '{name}'.");

        entity.Name = name;
        entity.Url = NormalizeUrl(request.Url);
        entity.LoginUrl = NormalizeOptional(request.LoginUrl, 500);
        entity.Notes = NormalizeOptional(request.Notes, 2000);
        entity.ScrapeConfig = NormalizeOptional(request.ScrapeConfig, 4000);
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Portal {id} no encontrado.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobPortalResponse> MarkScrapeQueuedAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Portal {id} no encontrado.");
        if (!entity.IsActive)
            throw new ValidationException("El portal está inactivo.");

        entity.LastRunAt = DateTime.UtcNow;
        entity.LastRunStatus = "queued_playwright";
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<JobPortalResponse> UpdateRunStatusAsync(
        int id, UpdateJobPortalRunStatusRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Portal {id} no encontrado.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new ValidationException("El status es obligatorio.");

        var status = request.Status.Trim();
        if (status.Length > 40)
            status = status[..40];

        var fullMessage = string.IsNullOrWhiteSpace(request.Message)
            ? null
            : request.Message.Trim();
        if (fullMessage is { Length: > 4000 })
            fullMessage = fullMessage[..4000];

        // Resumen corto en JobPortal.LastRunStatus (UI rápida).
        var summaryDetail = fullMessage;
        if (summaryDetail is { Length: > 180 })
            summaryDetail = summaryDetail[..180];

        var now = DateTime.UtcNow;
        entity.LastRunAt = now;
        entity.LastRunStatus = request.OfferCount is > 0
            ? $"{status}:{request.OfferCount}"
            : status;
        if (!string.IsNullOrWhiteSpace(summaryDetail))
            entity.LastRunStatus = $"{entity.LastRunStatus}|{summaryDetail}";
        if (entity.LastRunStatus.Length > 200)
            entity.LastRunStatus = entity.LastRunStatus[..200];
        entity.UpdatedAt = now;

        await UpsertScrapeLogAsync(
            id, status, fullMessage, request.OfferCount ?? 0,
            request.SavedInserted ?? 0, request.SavedUpdated ?? 0, now, cancellationToken);

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<JobPortalScrapeLogResponse>> GetScrapeLogsAsync(
        int portalId, int take = 50, CancellationToken cancellationToken = default)
    {
        _ = await _repository.GetByIdAsync(portalId, cancellationToken)
            ?? throw new NotFoundException($"Portal {portalId} no encontrado.");

        var logs = await _scrapeLogRepository.GetByPortalAsync(portalId, take, cancellationToken);
        return logs.Select(MapLog).ToList();
    }

    private async Task UpsertScrapeLogAsync(
        int portalId,
        string status,
        string? message,
        int offerCount,
        int savedInserted,
        int savedUpdated,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var isRunning = status.Equals("running", StringComparison.OrdinalIgnoreCase);
        var open = await _scrapeLogRepository.GetLatestOpenAsync(portalId, cancellationToken);

        if (isRunning)
        {
            if (open is not null)
            {
                open.Status = status;
                open.Message = message;
                open.OfferCount = offerCount;
                _scrapeLogRepository.Update(open);
                return;
            }

            await _scrapeLogRepository.AddAsync(new JobPortalScrapeLog
            {
                JobPortalId = portalId,
                StartedAt = now,
                FinishedAt = null,
                Status = status,
                Message = message,
                OfferCount = offerCount,
                SavedInserted = 0,
                SavedUpdated = 0
            }, cancellationToken);
            return;
        }

        if (open is not null)
        {
            open.FinishedAt = now;
            open.Status = status;
            open.Message = message;
            open.OfferCount = offerCount;
            open.SavedInserted = savedInserted;
            open.SavedUpdated = savedUpdated;
            _scrapeLogRepository.Update(open);
            return;
        }

        await _scrapeLogRepository.AddAsync(new JobPortalScrapeLog
        {
            JobPortalId = portalId,
            StartedAt = now,
            FinishedAt = now,
            Status = status,
            Message = message,
            OfferCount = offerCount,
            SavedInserted = savedInserted,
            SavedUpdated = savedUpdated
        }, cancellationToken);
    }

    private static JobPortalResponse Map(JobPortal entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Url = entity.Url,
        LoginUrl = entity.LoginUrl,
        Notes = entity.Notes,
        ScrapeConfig = entity.ScrapeConfig,
        IsActive = entity.IsActive,
        AutoScrapeEnabled = entity.AutoScrapeEnabled,
        LastRunAt = entity.LastRunAt,
        LastRunStatus = entity.LastRunStatus,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static JobPortalScrapeLogResponse MapLog(JobPortalScrapeLog entity) => new()
    {
        Id = entity.Id,
        JobPortalId = entity.JobPortalId,
        PortalName = entity.JobPortal?.Name ?? string.Empty,
        StartedAt = entity.StartedAt,
        FinishedAt = entity.FinishedAt,
        Status = entity.Status,
        Message = entity.Message,
        OfferCount = entity.OfferCount,
        SavedInserted = entity.SavedInserted,
        SavedUpdated = entity.SavedUpdated
    };

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("El nombre del portal es obligatorio.");
        var normalized = value.Trim();
        if (normalized.Length > 150)
            throw new ValidationException("El nombre no puede superar 150 caracteres.");
        return normalized;
    }

    private static string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("La URL del portal es obligatoria.");
        var normalized = value.Trim();
        if (normalized.Length > 500)
            throw new ValidationException("La URL no puede superar 500 caracteres.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ValidationException($"No puede superar {maxLength} caracteres.");
        return normalized;
    }
}
