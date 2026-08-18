using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;

    public ProjectService(IProjectRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(onlyActive, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<ProjectResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Proyecto {id} no encontrado."));

    public async Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        if (await _repository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ValidationException($"Ya existe el proyecto '{name}'.");

        var entity = new Project
        {
            Name = name,
            Description = description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProjectResponse> UpdateAsync(
        int id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Proyecto {id} no encontrado.");
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);

        if (await _repository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ValidationException($"Ya existe el proyecto '{name}'.");

        entity.Name = name;
        entity.Description = description;
        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Proyecto {id} no encontrado.");
        if (await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException("No se puede eliminar un proyecto que está en uso.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("El nombre es obligatorio.");
        var normalized = value.Trim();
        if (normalized.Length > 100)
            throw new ValidationException("El nombre no puede superar 100 caracteres.");
        return normalized;
    }

    private static string? NormalizeDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > 300)
            throw new ValidationException("La descripción no puede superar 300 caracteres.");
        return normalized;
    }

    private static ProjectResponse Map(Project entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
