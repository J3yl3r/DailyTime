using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces
{
    public interface INoteService
    {
        Task<IReadOnlyList<NoteResponse>> GetRootsByDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
        Task<NoteResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NoteResponse>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default);
        Task<NoteResponse> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);
        Task<NoteResponse> UpdateAsync(int id, UpdateNoteRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
