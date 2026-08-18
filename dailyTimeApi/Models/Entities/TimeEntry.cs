namespace dailyTimeApi.Models.Entities;

public class TimeEntry
{
    public int Id { get; set; }
    public int? TaskItemId { get; set; }
    public int? NoteId { get; set; }
    public DateOnly WorkDate { get; set; }
    public int DurationMinutes { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public TaskItem? TaskItem { get; set; }
    public Note? Note { get; set; }
}