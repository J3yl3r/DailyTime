namespace dailyTimeApi.Models.Response;

public class JobApplicationResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? FieldId { get; set; }
    public string? FieldName { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? StatusColor { get; set; }
    public DateOnly AppliedAt { get; set; }
    public string? Url { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
    public int? WorkExperienceId { get; set; }
    public string? WorkExperienceLabel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
