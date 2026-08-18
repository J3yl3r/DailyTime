namespace dailyTimeApi.Models.Entities;

public class JobApplication
{
    public int Id { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CareerCompany Company { get; set; } = null!;
    public CareerPosition Position { get; set; } = null!;
    public CareerLocation? Location { get; set; }
    public CareerField? Field { get; set; }
    public CareerApplicationStatus Status { get; set; } = null!;
    public WorkExperience? WorkExperience { get; set; }
}
