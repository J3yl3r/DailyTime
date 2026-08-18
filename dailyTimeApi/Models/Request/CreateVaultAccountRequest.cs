namespace dailyTimeApi.Models.Request;

public class CreateVaultAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
