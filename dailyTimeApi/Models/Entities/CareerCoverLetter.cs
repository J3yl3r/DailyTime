namespace dailyTimeApi.Models.Entities;

public class CareerCoverLetter
{
    public int Id { get; set; }
    public int CareerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    public string? Stack { get; set; }
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public CareerProfile CareerProfile { get; set; } = null!;
}
