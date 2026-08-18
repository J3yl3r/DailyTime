namespace dailyTimeApi.Models.Entities;

public class CareerTechnology
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<WorkExperienceTechnology> WorkExperiences { get; set; } = new List<WorkExperienceTechnology>();
}
