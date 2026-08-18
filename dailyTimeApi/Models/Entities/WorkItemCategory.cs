namespace dailyTimeApi.Models.Entities;

public class WorkItemCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
