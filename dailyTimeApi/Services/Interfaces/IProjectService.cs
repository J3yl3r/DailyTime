using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<ProjectResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
