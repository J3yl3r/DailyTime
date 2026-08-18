namespace dailyTimeApi.Models.Response;

public class JobPortalScrapeLogResponse
{
    public long Id { get; set; }
    public int JobPortalId { get; set; }
    public string PortalName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int OfferCount { get; set; }
    public int SavedInserted { get; set; }
    public int SavedUpdated { get; set; }
}
