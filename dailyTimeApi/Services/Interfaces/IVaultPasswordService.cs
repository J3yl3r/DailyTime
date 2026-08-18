using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IVaultPasswordService
{
    Task<IReadOnlyList<VaultPasswordResponse>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<VaultPasswordResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VaultPasswordResponse> CreateAsync(int accountId, CreateVaultPasswordRequest request, CancellationToken cancellationToken = default);
    Task<VaultPasswordResponse> UpdateAsync(int id, UpdateVaultPasswordRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
