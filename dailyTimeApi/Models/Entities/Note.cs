namespace dailyTimeApi.Models.Entities;

public class Note : IGoogleSyncedItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateOnly? WorkDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ParentNoteId { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int StatusId { get; set; }
    public int CategoryId { get; set; }
    public int? PersonId { get; set; }
    public int? ProjectId { get; set; }
    public int? CompanyId { get; set; }

    // --- Sincronización con Google Calendar ---
    public string? GoogleEventId { get; set; }
    public string? GoogleEtag { get; set; }
    public DateTime? GoogleSyncedAt { get; set; }
    public DateTime? GoogleUpdatedAt { get; set; }
    public string? SyncSource { get; set; }
    public string? GoogleColor { get; set; }

    public Note? Parent { get; set; }
    public WorkItemStatus? Status { get; set; }
    public WorkItemCategory? Category { get; set; }
    public Person? Person { get; set; }
    public Project? Project { get; set; }
    public Company? Company { get; set; }
    public ICollection<Note> Children { get; set; } = new List<Note>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
