namespace dailyTimeApi.Models.Entities;

public class WorkExperience
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int PositionId { get; set; }
    public int? LocationId { get; set; }
    public int? FieldId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Summary { get; set; }
    public string? Achievements { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CareerCompany Company { get; set; } = null!;
    public CareerPosition Position { get; set; } = null!;
    public CareerLocation? Location { get; set; }
    public CareerField? Field { get; set; }
    public ICollection<WorkExperienceTechnology> Technologies { get; set; } = new List<WorkExperienceTechnology>();
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
