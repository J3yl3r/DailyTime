using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces
{
    public interface ITaskItemService
    {
        Task<IReadOnlyList<TaskItemResponse>> GetRootsByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
        Task<TaskItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskItemResponse>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default);
        Task<TaskItemResponse> CreateAsync(CreateTaskItemRequest request, CancellationToken cancellationToken = default);
        Task<TaskItemResponse> UpdateAsync(int id, UpdateTaskItemRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
