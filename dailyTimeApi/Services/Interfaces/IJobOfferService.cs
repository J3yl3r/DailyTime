using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IJobOfferService
{
    Task<IReadOnlyList<JobOfferResponse>> GetAllAsync(
        JobOfferFilterRequest? filter = null,
        CancellationToken cancellationToken = default);
    Task<JobOfferMetaResponse> GetMetaAsync(CancellationToken cancellationToken = default);
    Task<JobOfferResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UpsertJobOffersResponse> UpsertBatchAsync(
        UpsertJobOffersRequest request, CancellationToken cancellationToken = default);
    Task<JobOfferResponse> UpdateStatusAsync(
        int id, UpdateJobOfferStatusRequest request, CancellationToken cancellationToken = default);
    Task<BulkJobOfferActionResponse> BulkUpdateStatusAsync(
        BulkJobOfferStatusRequest request, CancellationToken cancellationToken = default);
    Task<BulkJobOfferActionResponse> BulkDeleteAsync(
        BulkJobOfferDeleteRequest request, CancellationToken cancellationToken = default);
    Task ReorderAsync(
        ReorderJobOffersRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
