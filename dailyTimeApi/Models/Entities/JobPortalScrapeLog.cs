namespace dailyTimeApi.Models.Entities;

/// <summary>
/// Historial de corridas del scraper por portal (una fila por intento/estado).
/// </summary>
public class JobPortalScrapeLog
{
    public long Id { get; set; }
    public int JobPortalId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int OfferCount { get; set; }
    public int SavedInserted { get; set; }
    public int SavedUpdated { get; set; }

    public JobPortal JobPortal { get; set; } = null!;
}
