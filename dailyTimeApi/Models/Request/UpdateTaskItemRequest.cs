namespace dailyTimeApi.Models.Request;

public class UpdateTaskItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ParentTaskId { get; set; }
    public int StatusId { get; set; }
    public int CategoryId { get; set; }
    public int? PersonId { get; set; }
    public int? ProjectId { get; set; }
    public int? CompanyId { get; set; }
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
}
