using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyResponse>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<CompanyResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<CompanyResponse> UpdateAsync(int id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
