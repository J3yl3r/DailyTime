using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class WorkItemCategoryService : IWorkItemCategoryService
{
    private readonly IWorkItemCategoryRepository _repository;

    public WorkItemCategoryService(IWorkItemCategoryRepository repository) =>
        _repository = repository;

    public async Task<IReadOnlyList<WorkItemCategoryResponse>> GetAllAsync(
        string? itemType = null, CancellationToken cancellationToken = default)
    {
        var normalized = string.IsNullOrWhiteSpace(itemType)
            ? null
            : WorkItemStatusService.NormalizeItemType(itemType);
        var items = await _repository.GetAllAsync(normalized, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<WorkItemCategoryResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Categoría {id} no encontrada."));

    public async Task<WorkItemCategoryResponse> CreateAsync(
        CreateWorkItemCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var itemType = WorkItemStatusService.NormalizeItemType(request.ItemType);
        if (await _repository.ExistsByNameAsync(itemType, name, null, cancellationToken))
            throw new ValidationException($"Ya existe la categoría '{name}' para {itemType}.");

        var entity = new WorkItemCategory
        {
            Name = name,
            Description = description,
            ItemType = itemType,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<WorkItemCategoryResponse> UpdateAsync(
        int id, UpdateWorkItemCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Categoría {id} no encontrada.");
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var itemType = WorkItemStatusService.NormalizeItemType(request.ItemType);

        if (await _repository.ExistsByNameAsync(itemType, name, id, cancellationToken))
            throw new ValidationException($"Ya existe la categoría '{name}' para {itemType}.");
        if (entity.ItemType != itemType && await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException("No se puede cambiar el tipo de una categoría en uso.");

        entity.Name = name;
        entity.Description = description;
        entity.ItemType = itemType;
        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Categoría {id} no encontrada.");
        if (await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException("No se puede eliminar una categoría que está en uso.");
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

    private static string NormalizeDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("La descripción es obligatoria.");
        var normalized = value.Trim();
        if (normalized.Length > 300)
            throw new ValidationException("La descripción no puede superar 300 caracteres.");
        return normalized;
    }

    private static WorkItemCategoryResponse Map(WorkItemCategory entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        ItemType = entity.ItemType,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
