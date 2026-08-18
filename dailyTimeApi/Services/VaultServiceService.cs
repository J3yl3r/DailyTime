using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class VaultServiceService : IVaultServiceService
{
    private readonly IVaultServiceRepository _repository;

    public VaultServiceService(IVaultServiceRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<VaultServiceResponse>> GetAllAsync(
        bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(onlyActive, cancellationToken);
        var result = new List<VaultServiceResponse>();
        foreach (var item in items)
        {
            result.Add(await MapAsync(item, cancellationToken));
        }
        return result;
    }

    public async Task<VaultServiceResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Servicio de bóveda {id} no encontrado.");
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<VaultServiceResponse> CreateAsync(
        CreateVaultServiceRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        if (await _repository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ValidationException($"Ya existe el servicio '{name}'.");

        var entity = new VaultService
        {
            Name = name,
            Url = NormalizeOptional(request.Url, 500),
            Notes = NormalizeOptional(request.Notes, 500),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task<VaultServiceResponse> UpdateAsync(
        int id, UpdateVaultServiceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Servicio de bóveda {id} no encontrado.");
        var name = NormalizeName(request.Name);
        if (await _repository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ValidationException($"Ya existe el servicio '{name}'.");

        entity.Name = name;
        entity.Url = NormalizeOptional(request.Url, 500);
        entity.Notes = NormalizeOptional(request.Notes, 500);
        entity.IsActive = request.IsActive;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return await MapAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Servicio de bóveda {id} no encontrado.");
        if (await _repository.IsInUseAsync(id, cancellationToken))
            throw new ValidationException("No se puede eliminar un servicio que tiene contraseñas.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<VaultServiceResponse> MapAsync(
        VaultService entity, CancellationToken cancellationToken) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Url = entity.Url,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        PasswordCount = await _repository.CountPasswordsAsync(entity.Id, cancellationToken),
        CreatedAt = entity.CreatedAt
    };

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("El nombre del servicio es obligatorio.");
        var normalized = value.Trim();
        if (normalized.Length > 150)
            throw new ValidationException("El nombre no puede superar 150 caracteres.");
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
