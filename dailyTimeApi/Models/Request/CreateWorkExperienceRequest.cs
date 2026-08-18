namespace dailyTimeApi.Models.Request;

public class CreateWorkExperienceRequest
{
    public int CompanyId { get; set; }
    public int PositionId { get; set; }
    public int? LocationId { get; set; }
    public int? FieldId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Summary { get; set; }
    public string? Achievements { get; set; }
    public int[] TechnologyIds { get; set; } = [];
}
