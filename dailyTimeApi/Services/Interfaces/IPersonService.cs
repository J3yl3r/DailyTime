using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IPersonService
{
    Task<IReadOnlyList<PersonResponse>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<PersonResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PersonResponse> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default);
    Task<PersonResponse> UpdateAsync(int id, UpdatePersonRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
