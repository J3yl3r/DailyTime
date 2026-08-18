namespace dailyTimeApi.Models.Request;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
