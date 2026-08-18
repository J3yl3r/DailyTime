using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface ICareerLocationService
{
    Task<IReadOnlyList<CareerCatalogResponse>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<CareerCatalogResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CareerCatalogResponse> CreateAsync(CreateCareerCatalogRequest request, CancellationToken cancellationToken = default);
    Task<CareerCatalogResponse> UpdateAsync(int id, UpdateCareerCatalogRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
