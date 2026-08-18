using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces
{
    public interface ITimeEntryService
    {
        Task<IReadOnlyList<TimeEntryResponse>> GetByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TimeEntryResponse>> GetByTaskItemIdAsync(int taskItemId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TimeEntryResponse>> GetByNoteIdAsync(int noteId, CancellationToken cancellationToken = default);
        Task<TimeEntryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<TimeEntryResponse> CreateAsync(CreateTimeEntryRequest request, CancellationToken cancellationToken = default);
        Task<TimeEntryResponse> UpdateAsync(int id, UpdateTimeEntryRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
