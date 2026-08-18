using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class WorkExperienceService : IWorkExperienceService
{
    private readonly IWorkExperienceRepository _repository;
    private readonly ICareerCompanyRepository _companyRepository;
    private readonly ICareerPositionRepository _positionRepository;
    private readonly ICareerLocationRepository _locationRepository;
    private readonly ICareerFieldRepository _fieldRepository;
    private readonly ICareerTechnologyRepository _technologyRepository;

    public WorkExperienceService(
        IWorkExperienceRepository repository,
        ICareerCompanyRepository companyRepository,
        ICareerPositionRepository positionRepository,
        ICareerLocationRepository locationRepository,
        ICareerFieldRepository fieldRepository,
        ICareerTechnologyRepository technologyRepository)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _positionRepository = positionRepository;
        _locationRepository = locationRepository;
        _fieldRepository = fieldRepository;
        _technologyRepository = technologyRepository;
    }

    public async Task<IReadOnlyList<WorkExperienceResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<WorkExperienceResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Experiencia laboral {id} no encontrada."));

    public async Task<WorkExperienceResponse> CreateAsync(
        CreateWorkExperienceRequest request, CancellationToken cancellationToken = default)
    {
        var (endDate, summary, achievements, technologyIds) =
            await ValidateAsync(request.CompanyId, request.PositionId, request.LocationId,
                request.FieldId, request.StartDate, request.EndDate, request.IsCurrent,
                request.Summary, request.Achievements, request.TechnologyIds, cancellationToken);

        var now = DateTime.UtcNow;
        var entity = new WorkExperience
        {
            CompanyId = request.CompanyId,
            PositionId = request.PositionId,
            LocationId = request.LocationId,
            FieldId = request.FieldId,
            StartDate = request.StartDate,
            EndDate = endDate,
            IsCurrent = request.IsCurrent,
            Summary = summary,
            Achievements = achievements,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var technologyId in technologyIds)
            entity.Technologies.Add(new WorkExperienceTechnology { TechnologyId = technologyId });

        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetByIdAsync(entity.Id, cancellationToken)
            ?? throw new NotFoundException($"Experiencia laboral {entity.Id} no encontrada.");
        return Map(created);
    }

    public async Task<WorkExperienceResponse> UpdateAsync(
        int id, UpdateWorkExperienceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Experiencia laboral {id} no encontrada.");

        var (endDate, summary, achievements, technologyIds) =
            await ValidateAsync(request.CompanyId, request.PositionId, request.LocationId,
                request.FieldId, request.StartDate, request.EndDate, request.IsCurrent,
                request.Summary, request.Achievements, request.TechnologyIds, cancellationToken);

        entity.CompanyId = request.CompanyId;
        entity.PositionId = request.PositionId;
        entity.LocationId = request.LocationId;
        entity.FieldId = request.FieldId;
        entity.StartDate = request.StartDate;
        entity.EndDate = endDate;
        entity.IsCurrent = request.IsCurrent;
        entity.Summary = summary;
        entity.Achievements = achievements;
        entity.UpdatedAt = DateTime.UtcNow;
        SyncTechnologies(entity, technologyIds);

        await _repository.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Experiencia laboral {id} no encontrada.");
        return Map(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Experiencia laboral {id} no encontrada.");
        if (await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException(
                "No se puede eliminar una experiencia laboral que está en uso.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<(
        DateOnly? EndDate,
        string? Summary,
        string? Achievements,
        IReadOnlyList<int> TechnologyIds)>
        ValidateAsync(
            int companyId,
            int positionId,
            int? locationId,
            int? fieldId,
            DateOnly startDate,
            DateOnly? endDate,
            bool isCurrent,
            string? summary,
            string? achievements,
            int[]? technologyIds,
            CancellationToken cancellationToken)
    {
        if (!await _companyRepository.ExistsAsync(companyId, cancellationToken))
            throw new ValidationException($"La empresa {companyId} no existe.");
        if (!await _positionRepository.ExistsAsync(positionId, cancellationToken))
            throw new ValidationException($"El cargo {positionId} no existe.");
        if (locationId.HasValue &&
            !await _locationRepository.ExistsAsync(locationId.Value, cancellationToken))
            throw new ValidationException($"La ubicación {locationId.Value} no existe.");
        if (fieldId.HasValue &&
            !await _fieldRepository.ExistsAsync(fieldId.Value, cancellationToken))
            throw new ValidationException($"La carrera profesional {fieldId.Value} no existe.");

        var normalizedTechnologyIds = (technologyIds ?? [])
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        if (!await _technologyRepository.AllExistAsync(normalizedTechnologyIds, cancellationToken))
            throw new ValidationException("Una o más tecnologías no existen.");

        DateOnly? normalizedEndDate;
        if (isCurrent)
        {
            if (endDate.HasValue)
                throw new ValidationException(
                    "Si el puesto es actual, la fecha de fin debe ser nula.");
            normalizedEndDate = null;
        }
        else
        {
            if (endDate.HasValue && endDate.Value < startDate)
                throw new ValidationException(
                    "La fecha de fin debe ser mayor o igual a la fecha de inicio.");
            normalizedEndDate = endDate;
        }

        return (
            normalizedEndDate,
            NormalizeOptional(summary, 2000, "resumen"),
            NormalizeOptional(achievements, 4000, "logros"),
            normalizedTechnologyIds);
    }

    private static void SyncTechnologies(WorkExperience entity, IReadOnlyList<int> technologyIds)
    {
        var desired = technologyIds.ToHashSet();
        var toRemove = entity.Technologies.Where(x => !desired.Contains(x.TechnologyId)).ToList();
        foreach (var item in toRemove)
            entity.Technologies.Remove(item);

        var existing = entity.Technologies.Select(x => x.TechnologyId).ToHashSet();
        foreach (var technologyId in desired.Where(id => !existing.Contains(id)))
            entity.Technologies.Add(new WorkExperienceTechnology { TechnologyId = technologyId });
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ValidationException(
                $"El {fieldLabel} no puede superar {maxLength} caracteres.");
        return normalized;
    }

    private static WorkExperienceResponse Map(WorkExperience entity)
    {
        var technologies = entity.Technologies
            .OrderBy(x => x.Technology?.Name ?? string.Empty)
            .ToList();

        return new WorkExperienceResponse
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            CompanyName = entity.Company?.Name ?? string.Empty,
            PositionId = entity.PositionId,
            PositionName = entity.Position?.Name ?? string.Empty,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.Name,
            FieldId = entity.FieldId,
            FieldName = entity.Field?.Name,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsCurrent = entity.IsCurrent,
            Summary = entity.Summary,
            Achievements = entity.Achievements,
            TechnologyIds = technologies.Select(x => x.TechnologyId).ToList(),
            TechnologyNames = technologies
                .Select(x => x.Technology?.Name ?? string.Empty)
                .Where(x => x.Length > 0)
                .ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
