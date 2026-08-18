using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class JobApplicationService : IJobApplicationService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IWorkExperienceRepository _workExperienceRepository;
    private readonly ICareerCompanyRepository _companyRepository;
    private readonly ICareerPositionRepository _positionRepository;
    private readonly ICareerLocationRepository _locationRepository;
    private readonly ICareerFieldRepository _fieldRepository;
    private readonly ICareerApplicationStatusRepository _statusRepository;

    public JobApplicationService(
        IJobApplicationRepository repository,
        IWorkExperienceRepository workExperienceRepository,
        ICareerCompanyRepository companyRepository,
        ICareerPositionRepository positionRepository,
        ICareerLocationRepository locationRepository,
        ICareerFieldRepository fieldRepository,
        ICareerApplicationStatusRepository statusRepository)
    {
        _repository = repository;
        _workExperienceRepository = workExperienceRepository;
        _companyRepository = companyRepository;
        _positionRepository = positionRepository;
        _locationRepository = locationRepository;
        _fieldRepository = fieldRepository;
        _statusRepository = statusRepository;
    }

    public async Task<IReadOnlyList<JobApplicationResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<JobApplicationResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Postulación {id} no encontrada."));

    public async Task<JobApplicationResponse> CreateAsync(
        CreateJobApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var (url, contact, notes, workExperienceId) =
            await ValidateAsync(request.CompanyId, request.PositionId, request.LocationId,
                request.FieldId, request.StatusId, request.Url, request.Contact, request.Notes,
                request.WorkExperienceId, cancellationToken);

        var now = DateTime.UtcNow;
        var entity = new JobApplication
        {
            CompanyId = request.CompanyId,
            PositionId = request.PositionId,
            LocationId = request.LocationId,
            FieldId = request.FieldId,
            StatusId = request.StatusId,
            AppliedAt = request.AppliedAt,
            Url = url,
            Contact = contact,
            Notes = notes,
            WorkExperienceId = workExperienceId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetByIdAsync(entity.Id, cancellationToken)
            ?? throw new NotFoundException($"Postulación {entity.Id} no encontrada.");
        return Map(created);
    }

    public async Task<JobApplicationResponse> UpdateAsync(
        int id, UpdateJobApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Postulación {id} no encontrada.");

        var (url, contact, notes, workExperienceId) =
            await ValidateAsync(request.CompanyId, request.PositionId, request.LocationId,
                request.FieldId, request.StatusId, request.Url, request.Contact, request.Notes,
                request.WorkExperienceId, cancellationToken);

        entity.CompanyId = request.CompanyId;
        entity.PositionId = request.PositionId;
        entity.LocationId = request.LocationId;
        entity.FieldId = request.FieldId;
        entity.StatusId = request.StatusId;
        entity.AppliedAt = request.AppliedAt;
        entity.Url = url;
        entity.Contact = contact;
        entity.Notes = notes;
        entity.WorkExperienceId = workExperienceId;
        entity.WorkExperience = null;
        entity.Company = null!;
        entity.Position = null!;
        entity.Location = null;
        entity.Field = null;
        entity.Status = null!;
        entity.UpdatedAt = DateTime.UtcNow;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Postulación {id} no encontrada.");
        return Map(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Postulación {id} no encontrada.");
        entity.WorkExperience = null;
        entity.Company = null!;
        entity.Position = null!;
        entity.Location = null;
        entity.Field = null;
        entity.Status = null!;
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<BulkJobApplicationActionResponse> BulkDeleteAsync(
        BulkJobApplicationDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.Ids?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            throw new ValidationException("Debes indicar al menos un id.");

        var affected = await _repository.BulkDeleteAsync(ids, cancellationToken);
        return new BulkJobApplicationActionResponse { Affected = affected };
    }

    public async Task EnsureFromOfferAsync(JobOffer offer, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(offer.Url) ? null : offer.Url.Trim();
        if (!string.IsNullOrWhiteSpace(url)
            && await _repository.ExistsByUrlAsync(url, cancellationToken))
            return;

        var companyName = string.IsNullOrWhiteSpace(offer.Company) ? "Sin empresa" : offer.Company.Trim();
        var positionName = string.IsNullOrWhiteSpace(offer.Title) ? "Sin cargo" : offer.Title.Trim();
        if (companyName.Length > 100) companyName = companyName[..100];
        if (positionName.Length > 100) positionName = positionName[..100];

        var company = await EnsureCompanyAsync(companyName, cancellationToken);
        var position = await EnsurePositionAsync(positionName, cancellationToken);
        var locationId = await EnsureLocationIdAsync(offer.Location, cancellationToken);
        var status = await EnsureDefaultStatusAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var notesParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(offer.JobPortal?.Name))
            notesParts.Add($"Origen: {offer.JobPortal.Name}");
        else if (offer.JobPortalId > 0)
            notesParts.Add($"Origen oferta #{offer.Id}");
        if (!string.IsNullOrWhiteSpace(offer.Description))
            notesParts.Add(offer.Description.Length > 1500
                ? offer.Description.Trim()[..1500] + "…"
                : offer.Description.Trim());
        else if (!string.IsNullOrWhiteSpace(offer.DescriptionSnippet))
            notesParts.Add(offer.DescriptionSnippet.Trim());

        var notes = notesParts.Count == 0
            ? null
            : string.Join(" · ", notesParts);
        if (notes is { Length: > 2000 })
            notes = notes[..2000];

        await _repository.AddAsync(new JobApplication
        {
            CompanyId = company.Id,
            PositionId = position.Id,
            LocationId = locationId,
            StatusId = status.Id,
            AppliedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            Url = url is { Length: > 500 } ? url[..500] : url,
            Notes = notes,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CareerCompany> EnsureCompanyAsync(
        string name, CancellationToken cancellationToken)
    {
        var existing = await _companyRepository.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
            return existing;

        var entity = new CareerCompany
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _companyRepository.AddAsync(entity, cancellationToken);
        await _companyRepository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task<CareerPosition> EnsurePositionAsync(
        string name, CancellationToken cancellationToken)
    {
        var existing = await _positionRepository.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
            return existing;

        var entity = new CareerPosition
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _positionRepository.AddAsync(entity, cancellationToken);
        await _positionRepository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task<int?> EnsureLocationIdAsync(
        string? locationName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return null;

        var name = locationName.Trim();
        if (name.Length > 100) name = name[..100];

        var existing = await _locationRepository.FindByNameAsync(name, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var entity = new CareerLocation
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _locationRepository.AddAsync(entity, cancellationToken);
        await _locationRepository.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    private async Task<CareerApplicationStatus> EnsureDefaultStatusAsync(
        CancellationToken cancellationToken)
    {
        var statuses = await _statusRepository.GetAllAsync(onlyActive: true, cancellationToken);
        var preferred = statuses
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .FirstOrDefault(x =>
                x.Name.Contains("enviad", StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains("postul", StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains("aplicad", StringComparison.OrdinalIgnoreCase));

        if (preferred is not null)
            return preferred;

        if (statuses.Count > 0)
            return statuses.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).First();

        var created = new CareerApplicationStatus
        {
            Name = "Enviada",
            Color = "#2563EB",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _statusRepository.AddAsync(created, cancellationToken);
        await _statusRepository.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<(
        string? Url,
        string? Contact,
        string? Notes,
        int? WorkExperienceId)>
        ValidateAsync(
            int companyId,
            int positionId,
            int? locationId,
            int? fieldId,
            int statusId,
            string? url,
            string? contact,
            string? notes,
            int? workExperienceId,
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
        if (!await _statusRepository.ExistsAsync(statusId, cancellationToken))
            throw new ValidationException($"El estado de postulación {statusId} no existe.");

        if (workExperienceId.HasValue)
        {
            _ = await _workExperienceRepository.GetByIdAsync(workExperienceId.Value, cancellationToken)
                ?? throw new ValidationException(
                    $"La experiencia laboral {workExperienceId.Value} no existe.");
        }

        return (
            NormalizeOptional(url, 500, "url"),
            NormalizeOptional(contact, 200, "contacto"),
            NormalizeOptional(notes, 2000, "notas"),
            workExperienceId);
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

    private static JobApplicationResponse Map(JobApplication entity) => new()
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
        StatusId = entity.StatusId,
        StatusName = entity.Status?.Name ?? string.Empty,
        StatusColor = entity.Status?.Color,
        AppliedAt = entity.AppliedAt,
        Url = entity.Url,
        Contact = entity.Contact,
        Notes = entity.Notes,
        WorkExperienceId = entity.WorkExperienceId,
        WorkExperienceLabel = entity.WorkExperience is null
            ? null
            : $"{entity.WorkExperience.Position?.Name ?? string.Empty} @ {entity.WorkExperience.Company?.Name ?? string.Empty}",
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
