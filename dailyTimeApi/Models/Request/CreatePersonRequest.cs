namespace dailyTimeApi.Models.Request;

public class CreatePersonRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
