namespace dailyTimeApi.Models.Entities;

public class VaultAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<VaultPassword> Passwords { get; set; } = new List<VaultPassword>();
}
