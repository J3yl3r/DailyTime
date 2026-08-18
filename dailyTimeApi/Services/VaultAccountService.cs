using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class VaultAccountService : IVaultAccountService
{
    private readonly IVaultAccountRepository _repository;

    public VaultAccountService(IVaultAccountRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<VaultAccountResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        var result = new List<VaultAccountResponse>(items.Count);
        foreach (var item in items)
        {
            var count = await _repository.CountPasswordsAsync(item.Id, cancellationToken);
            result.Add(Map(item, count));
        }
        return result;
    }

    public async Task<VaultAccountResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Cuenta de bóveda {id} no encontrada.");
        var count = await _repository.CountPasswordsAsync(id, cancellationToken);
        return Map(entity, count);
    }

    public async Task<VaultAccountResponse> CreateAsync(
        CreateVaultAccountRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);
        if (await _repository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ValidationException($"Ya existe la cuenta '{name}'.");

        var now = DateTime.UtcNow;
        var entity = new VaultAccount
        {
            Name = name,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity, 0);
    }

    public async Task<VaultAccountResponse> UpdateAsync(
        int id, UpdateVaultAccountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Cuenta de bóveda {id} no encontrada.");
        var name = NormalizeName(request.Name);
        var description = NormalizeDescription(request.Description);

        if (await _repository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ValidationException($"Ya existe la cuenta '{name}'.");

        entity.Name = name;
        entity.Description = description;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        var count = await _repository.CountPasswordsAsync(id, cancellationToken);
        return Map(entity, count);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Cuenta de bóveda {id} no encontrada.");
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

    private static VaultAccountResponse Map(VaultAccount entity, int passwordCount) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        PasswordCount = passwordCount,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
