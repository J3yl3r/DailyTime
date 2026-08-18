namespace dailyTimeApi.Models.Entities;

public class CareerField
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}
