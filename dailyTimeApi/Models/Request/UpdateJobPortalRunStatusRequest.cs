namespace dailyTimeApi.Models.Request;

public class UpdateJobPortalRunStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? OfferCount { get; set; }
    public int? SavedInserted { get; set; }
    public int? SavedUpdated { get; set; }
}
