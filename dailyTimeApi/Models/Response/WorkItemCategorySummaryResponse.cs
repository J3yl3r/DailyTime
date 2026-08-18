namespace dailyTimeApi.Models.Response;

public class WorkItemCategorySummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
