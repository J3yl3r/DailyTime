using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IWorkExperienceService
{
    Task<IReadOnlyList<WorkExperienceResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WorkExperienceResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkExperienceResponse> CreateAsync(CreateWorkExperienceRequest request, CancellationToken cancellationToken = default);
    Task<WorkExperienceResponse> UpdateAsync(int id, UpdateWorkExperienceRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
