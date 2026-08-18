namespace dailyTimeApi.Models.Response;

public class WorkExperienceResponse
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
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Summary { get; set; }
    public string? Achievements { get; set; }
    public IReadOnlyList<int> TechnologyIds { get; set; } = [];
    public IReadOnlyList<string> TechnologyNames { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
