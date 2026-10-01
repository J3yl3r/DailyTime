namespace dailyTimeApi.Models.Response;

public class NoteResponse
{
    public int Id { get; set; }
    public int? ParentNoteId { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateOnly? WorkDate { get; set; }
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
    /// <summary>Evento espejo en Google Calendar, si el elemento está sincronizado.</summary>
    public string? GoogleEventId { get; set; }
    /// <summary>local | google — dónde nació el elemento.</summary>
    public string? SyncSource { get; set; }
    /// <summary>Color del evento en Google, en hexadecimal; solo en lo que viene de allí.</summary>
    public string? GoogleColor { get; set; }
}
