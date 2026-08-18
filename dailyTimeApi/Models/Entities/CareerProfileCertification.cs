namespace dailyTimeApi.Models.Entities;

public class CareerProfileCertification
{
    public int Id { get; set; }
    public int CareerProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Year { get; set; }
    public int SortOrder { get; set; }
    public CareerProfile CareerProfile { get; set; } = null!;
}
