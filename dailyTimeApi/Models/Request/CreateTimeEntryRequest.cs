namespace dailyTimeApi.Models.Request;

public class CreateTimeEntryRequest
{
    public int? TaskItemId { get; set; }
    public int? NoteId { get; set; }
    public DateOnly WorkDate { get; set; }
    public int DurationMinutes { get; set; }
    public string? Description { get; set; }
}