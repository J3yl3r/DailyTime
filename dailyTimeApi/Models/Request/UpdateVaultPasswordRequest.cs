namespace dailyTimeApi.Models.Request;

public class UpdateVaultPasswordRequest
{
    public int ServiceId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
