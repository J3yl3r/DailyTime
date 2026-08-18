using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IJobApplicationService
{
    Task<IReadOnlyList<JobApplicationResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<JobApplicationResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<JobApplicationResponse> CreateAsync(CreateJobApplicationRequest request, CancellationToken cancellationToken = default);
    Task<JobApplicationResponse> UpdateAsync(int id, UpdateJobApplicationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<BulkJobApplicationActionResponse> BulkDeleteAsync(
        BulkJobApplicationDeleteRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Crea una postulación a partir de una oferta (empresa/cargo/estado reutilizables).
    /// Idempotente por URL de la oferta.
    /// </summary>
    Task EnsureFromOfferAsync(JobOffer offer, CancellationToken cancellationToken = default);
}
