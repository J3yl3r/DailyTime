namespace dailyTimeApi.Models.Entities;

public class WorkItemStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#64748B";
    public bool IsFinal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ItemType { get; set; } = string.Empty;

    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
