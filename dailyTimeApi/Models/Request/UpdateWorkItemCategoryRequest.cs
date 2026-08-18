namespace dailyTimeApi.Models.Request;

public class UpdateWorkItemCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
