namespace dailyTimeApi.Models.Entities;

public class CareerApplicationStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#64748B";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}
