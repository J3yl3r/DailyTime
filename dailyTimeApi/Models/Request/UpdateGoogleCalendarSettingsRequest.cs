namespace dailyTimeApi.Models.Request;

public class UpdateGoogleCalendarSettingsRequest
{
    public bool SyncEnabled { get; set; }
    public bool SyncTimedTasks { get; set; }
    public bool SyncAllDayTasks { get; set; }
    public bool SyncTimedNotes { get; set; }

    /// <summary>Calendario destino; null deja el actual.</summary>
    public string? CalendarId { get; set; }

    /// <summary>Zona en la que se interpretan las horas; null deja la actual.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Días hacia atrás y hacia adelante que se sincronizan.</summary>
    public int PastDays { get; set; }
    public int FutureDays { get; set; }
}
