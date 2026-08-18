using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface ICareerProfileService
{
    Task<CareerProfileResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<CareerProfileResponse> UpsertAsync(
        UpsertCareerProfileRequest request, CancellationToken cancellationToken = default);
}
