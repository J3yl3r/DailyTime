namespace dailyTimeApi.Models.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ParentTaskId { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int StatusId { get; set; }
    public int CategoryId { get; set; }
    public int? PersonId { get; set; }
    public int? ProjectId { get; set; }
    public int? CompanyId { get; set; }

    public TaskItem? Parent { get; set; }
    public WorkItemStatus? Status { get; set; }
    public WorkItemCategory? Category { get; set; }
    public Person? Person { get; set; }
    public Project? Project { get; set; }
    public Company? Company { get; set; }
    public ICollection<TaskItem> Children { get; set; } = new List<TaskItem>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
