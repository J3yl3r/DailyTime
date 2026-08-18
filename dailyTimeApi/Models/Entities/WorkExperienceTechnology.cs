namespace dailyTimeApi.Models.Entities;

public class WorkExperienceTechnology
{
    public int WorkExperienceId { get; set; }
    public int TechnologyId { get; set; }

    public WorkExperience WorkExperience { get; set; } = null!;
    public CareerTechnology Technology { get; set; } = null!;
}
