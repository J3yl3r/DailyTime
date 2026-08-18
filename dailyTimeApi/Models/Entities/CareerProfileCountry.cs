namespace dailyTimeApi.Models.Entities;

public class CareerProfileCountry
{
    public int Id { get; set; }
    public int CareerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public CareerProfile CareerProfile { get; set; } = null!;
}
