namespace dailyTimeApi.Models.Request;

public class CreateJobPortalRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LoginUrl { get; set; }
    public string? Notes { get; set; }
    public string? ScrapeConfig { get; set; }
    public bool IsActive { get; set; } = true;
}
