namespace dailyTimeApi.Models.Request;

public class UpdateTimeEntryRequest
{
    public DateOnly WorkDate { get; set; }
    public int DurationMinutes { get; set; }
    public string? Description { get; set; }
}