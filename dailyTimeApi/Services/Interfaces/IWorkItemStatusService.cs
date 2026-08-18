using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IWorkItemStatusService
{
    Task<IReadOnlyList<WorkItemStatusResponse>> GetAllAsync(string? itemType = null, CancellationToken cancellationToken = default);
    Task<WorkItemStatusResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkItemStatusResponse> CreateAsync(CreateWorkItemStatusRequest request, CancellationToken cancellationToken = default);
    Task<WorkItemStatusResponse> UpdateAsync(int id, UpdateWorkItemStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
