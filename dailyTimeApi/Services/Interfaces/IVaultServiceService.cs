using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IVaultServiceService
{
    Task<IReadOnlyList<VaultServiceResponse>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<VaultServiceResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VaultServiceResponse> CreateAsync(CreateVaultServiceRequest request, CancellationToken cancellationToken = default);
    Task<VaultServiceResponse> UpdateAsync(int id, UpdateVaultServiceRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
