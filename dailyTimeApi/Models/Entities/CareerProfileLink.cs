namespace dailyTimeApi.Models.Entities;

public class CareerProfileLink
{
    public int Id { get; set; }
    public int CareerProfileId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public CareerProfile CareerProfile { get; set; } = null!;
}
