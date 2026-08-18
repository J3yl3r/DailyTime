namespace dailyTimeApi.Models.Response;

public class TaskItemResponse
{
    public int Id { get; set; }
    public int? ParentTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int SortOrder { get; set; }
    public int StatusId { get; set; }
    public WorkItemStatusSummaryResponse? Status { get; set; }
    public int CategoryId { get; set; }
    public WorkItemCategorySummaryResponse? Category { get; set; }
    public int? PersonId { get; set; }
    public PersonSummaryResponse? Person { get; set; }
    public int? ProjectId { get; set; }
    public ProjectSummaryResponse? Project { get; set; }
    public int? CompanyId { get; set; }
    public CompanySummaryResponse? Company { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
