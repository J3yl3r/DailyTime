using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IVaultAccountService
{
    Task<IReadOnlyList<VaultAccountResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VaultAccountResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VaultAccountResponse> CreateAsync(CreateVaultAccountRequest request, CancellationToken cancellationToken = default);
    Task<VaultAccountResponse> UpdateAsync(int id, UpdateVaultAccountRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
