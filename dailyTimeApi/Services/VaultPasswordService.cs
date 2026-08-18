using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class VaultPasswordService : IVaultPasswordService
{
    private readonly IVaultPasswordRepository _repository;
    private readonly IVaultAccountRepository _accountRepository;
    private readonly IVaultServiceRepository _serviceRepository;

    public VaultPasswordService(
        IVaultPasswordRepository repository,
        IVaultAccountRepository accountRepository,
        IVaultServiceRepository serviceRepository)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<IReadOnlyList<VaultPasswordResponse>> GetByAccountIdAsync(
        int accountId, CancellationToken cancellationToken = default)
    {
        _ = await _accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException($"Cuenta de bóveda {accountId} no encontrada.");
        var items = await _repository.GetByAccountIdAsync(accountId, cancellationToken);
        return items.Select(x => Map(x)).ToList();
    }

    public async Task<VaultPasswordResponse> GetByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        Map(await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Contraseña {id} no encontrada."));

    public async Task<VaultPasswordResponse> CreateAsync(
        int accountId, CreateVaultPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await _accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException($"Cuenta de bóveda {accountId} no encontrada.");
        var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Servicio de bóveda {request.ServiceId} no encontrado.");

        var now = DateTime.UtcNow;
        var entity = new VaultPassword
        {
            AccountId = accountId,
            ServiceId = service.Id,
            Username = NormalizeRequired(request.Username, "El usuario es obligatorio.", 200),
            Password = NormalizeRequired(request.Password, "La contraseña es obligatoria.", 500),
            Url = NormalizeOptional(request.Url, 500) ?? service.Url,
            Notes = NormalizeOptional(request.Notes, 1000),
            Tags = NormalizeTags(request.Tags),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity, service.Name);
    }

    public async Task<VaultPasswordResponse> UpdateAsync(
        int id, UpdateVaultPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Contraseña {id} no encontrada.");
        var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken)
            ?? throw new NotFoundException($"Servicio de bóveda {request.ServiceId} no encontrado.");

        entity.ServiceId = service.Id;
        entity.Username = NormalizeRequired(request.Username, "El usuario es obligatorio.", 200);
        entity.Password = NormalizeRequired(request.Password, "La contraseña es obligatoria.", 500);
        entity.Url = NormalizeOptional(request.Url, 500) ?? service.Url;
        entity.Notes = NormalizeOptional(request.Notes, 1000);
        entity.Tags = NormalizeTags(request.Tags);
        entity.UpdatedAt = DateTime.UtcNow;
        // No asignar navegación AsNoTracking: EF intentaría INSERT en VaultService.
        entity.Service = null!;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity, service.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Contraseña {id} no encontrada.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeRequired(string? value, string emptyMessage, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(emptyMessage);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ValidationException($"No puede superar {maxLength} caracteres.");
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

    private static string? NormalizeTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var tags = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.ToLowerInvariant())
            .Where(tag => tag.Length > 0)
            .Distinct()
            .ToList();
        if (tags.Count == 0)
            return null;
        var joined = string.Join(", ", tags);
        if (joined.Length > 500)
            throw new ValidationException("Las etiquetas no pueden superar 500 caracteres.");
        return joined;
    }

    private static VaultPasswordResponse Map(VaultPassword entity, string? serviceName = null) => new()
    {
        Id = entity.Id,
        AccountId = entity.AccountId,
        ServiceId = entity.ServiceId,
        ServiceName = serviceName ?? entity.Service?.Name ?? string.Empty,
        Username = entity.Username,
        Password = entity.Password,
        Url = entity.Url,
        Notes = entity.Notes,
        Tags = entity.Tags,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
