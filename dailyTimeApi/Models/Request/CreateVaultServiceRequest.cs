namespace dailyTimeApi.Models.Request;

public class CreateVaultServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
