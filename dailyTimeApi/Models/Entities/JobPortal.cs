namespace dailyTimeApi.Models.Entities;

/// <summary>
/// Portal / fuente de ofertas laborales para captura con bot (Playwright).
/// </summary>
public class JobPortal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LoginUrl { get; set; }
    public string? Notes { get; set; }
    /// <summary>JSON con selectores / parámetros de scraping para Playwright.</summary>
    public string? ScrapeConfig { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
