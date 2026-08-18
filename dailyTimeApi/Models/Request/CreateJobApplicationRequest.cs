namespace dailyTimeApi.Models.Request;

public class CreateJobApplicationRequest
{
    public int CompanyId { get; set; }
    public int PositionId { get; set; }
    public int? LocationId { get; set; }
    public int? FieldId { get; set; }
    public int StatusId { get; set; }
    public DateOnly AppliedAt { get; set; }
    public string? Url { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
    public int? WorkExperienceId { get; set; }
}
