using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IScrapeScheduleService
{
    Task<ScrapeScheduleResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<ScrapeScheduleResponse> UpdateAsync(
        UpdateScrapeScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ScrapeScheduleResponse> MarkSlotAsync(
        MarkScrapeSlotRequest request, CancellationToken cancellationToken = default);
}
