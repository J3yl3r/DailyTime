namespace dailyTimeApi.Models.Entities;

public class VaultService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<VaultPassword> Passwords { get; set; } = new List<VaultPassword>();
}
