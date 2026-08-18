using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IWorkItemCategoryService
{
    Task<IReadOnlyList<WorkItemCategoryResponse>> GetAllAsync(string? itemType = null, CancellationToken cancellationToken = default);
    Task<WorkItemCategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkItemCategoryResponse> CreateAsync(CreateWorkItemCategoryRequest request, CancellationToken cancellationToken = default);
    Task<WorkItemCategoryResponse> UpdateAsync(int id, UpdateWorkItemCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
