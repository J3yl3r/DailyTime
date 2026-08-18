namespace dailyTimeApi.Models.Request;

public class CreateNoteRequest
{
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateOnly? WorkDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ParentNoteId { get; set; }
    public int? StatusId { get; set; }
    public int? CategoryId { get; set; }
    public int? PersonId { get; set; }
    public int? ProjectId { get; set; }
    public int? CompanyId { get; set; }
    public int SortOrder { get; set; } = 0;
    public int DurationMinutes { get; set; } = 0;
}
