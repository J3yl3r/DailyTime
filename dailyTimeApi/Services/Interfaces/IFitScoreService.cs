using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;

namespace dailyTimeApi.Services.Interfaces;

public interface IFitScoreService
{
    Task<FitScoreResponse> EvaluateAsync(
        EvaluateFitRequest request,
        CancellationToken cancellationToken = default);
}
