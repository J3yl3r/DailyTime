namespace dailyTimeApi.Models.Response;

public class OfferAiQueueResponse
{
    /// <summary>Ofertas que quedaron en cola para analizar.</summary>
    public int Queued { get; set; }
    public OfferAiStatusResponse Status { get; set; } = new();
}
