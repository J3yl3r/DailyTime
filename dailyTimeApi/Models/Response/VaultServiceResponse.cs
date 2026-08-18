namespace dailyTimeApi.Models.Response;

public class VaultServiceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int PasswordCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
