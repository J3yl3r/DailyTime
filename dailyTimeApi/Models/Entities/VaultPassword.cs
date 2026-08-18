namespace dailyTimeApi.Models.Entities;

public class VaultPassword
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int ServiceId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public VaultAccount? Account { get; set; }
    public VaultService? Service { get; set; }
}
