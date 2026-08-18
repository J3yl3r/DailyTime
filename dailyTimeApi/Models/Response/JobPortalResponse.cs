namespace dailyTimeApi.Models.Response;

public class JobPortalResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LoginUrl { get; set; }
    public string? Notes { get; set; }
    public string? ScrapeConfig { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
