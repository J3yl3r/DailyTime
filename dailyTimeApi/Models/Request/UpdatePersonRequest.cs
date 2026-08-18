namespace dailyTimeApi.Models.Request;

public class UpdatePersonRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
