namespace dailyTimeApi.Models.Response;

public class WorkItemStatusResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
