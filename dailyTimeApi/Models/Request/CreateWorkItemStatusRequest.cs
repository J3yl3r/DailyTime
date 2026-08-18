namespace dailyTimeApi.Models.Request;

public class CreateWorkItemStatusRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#64748B";
    public bool IsFinal { get; set; }
    public string ItemType { get; set; } = string.Empty;
}
