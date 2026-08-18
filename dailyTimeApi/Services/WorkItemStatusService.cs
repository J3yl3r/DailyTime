using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class WorkItemStatusService : IWorkItemStatusService
{
    public const string ItemTypeTask = "task";
    public const string ItemTypeNote = "note";

    private readonly IWorkItemStatusRepository _repository;

    public WorkItemStatusService(IWorkItemStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<WorkItemStatusResponse>> GetAllAsync(
        string? itemType = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeOptionalItemType(itemType);
        var items = await _repository.GetAllAsync(normalizedType, cancellationToken);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<WorkItemStatusResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Estado {id} no encontrado.");
        return MapToResponse(item);
    }

    public async Task<WorkItemStatusResponse> CreateAsync(
        CreateWorkItemStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var color = NormalizeColor(request.Color);
        var itemType = NormalizeItemType(request.ItemType);

        var entity = new WorkItemStatus
        {
            Name = name,
            Description = description,
            Color = color,
            IsFinal = request.IsFinal,
            ItemType = itemType,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return MapToResponse(entity);
    }

    public async Task<WorkItemStatusResponse> UpdateAsync(
        int id,
        UpdateWorkItemStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Estado {id} no encontrado.");

        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        var color = NormalizeColor(request.Color);
        var itemType = NormalizeItemType(request.ItemType);

        if (!string.Equals(entity.ItemType, itemType, StringComparison.OrdinalIgnoreCase)
            && await _repository.IsInUseAsync(id, cancellationToken))
        {
            throw new ValidationException(
                "No se puede cambiar el tipo de un estado que ya está asignado a tareas o notas.");
        }

        entity.Name = name;
        entity.Description = description;
        entity.Color = color;
        entity.IsFinal = request.IsFinal;
        entity.ItemType = itemType;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return MapToResponse(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Estado {id} no encontrado.");

        if (await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException("No se puede eliminar un estado que está en uso.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public static string NormalizeItemType(string? itemType)
    {
        var normalized = itemType?.Trim().ToLowerInvariant();
        if (normalized is not (ItemTypeTask or ItemTypeNote))
            throw new ValidationException("ItemType debe ser 'task' o 'note'.");
        return normalized;
    }

    private static string? NormalizeOptionalItemType(string? itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return null;
        return NormalizeItemType(itemType);
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("El nombre del estado es obligatorio.");

        var normalized = name.Trim();
        if (normalized.Length > 100)
            throw new ValidationException("El nombre no puede superar 100 caracteres.");
        return normalized;
    }

    private static string NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ValidationException("La descripción del estado es obligatoria.");

        var normalized = description.Trim();
        if (normalized.Length > 300)
            throw new ValidationException("La descripción no puede superar 300 caracteres.");
        return normalized;
    }

    private static string NormalizeColor(string? color)
    {
        var normalized = color?.Trim().ToUpperInvariant();
        if (normalized is null ||
            !System.Text.RegularExpressions.Regex.IsMatch(normalized, "^#[0-9A-F]{6}$"))
            throw new ValidationException("El color debe tener formato hexadecimal #RRGGBB.");
        return normalized;
    }

    private static WorkItemStatusResponse MapToResponse(WorkItemStatus entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Color = entity.Color,
        IsFinal = entity.IsFinal,
        ItemType = entity.ItemType,
        CreatedAt = entity.CreatedAt
    };
}
