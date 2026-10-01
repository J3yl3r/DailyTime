namespace dailyTimeApi.Models.Entities;

/// <summary>
/// Cuenta de Google conectada para sincronizar el calendario. Una sola fila, como
/// <see cref="ScrapeSchedule"/>: la app es de escritorio y no tiene usuarios múltiples.
/// Los tokens viven aquí porque la base es local; no exponer la API fuera de localhost.
/// </summary>
public class GoogleCalendarAccount
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>Calendario destino. "primary" es el principal de la cuenta.</summary>
    public string CalendarId { get; set; } = "primary";

    /// <summary>
    /// Corte del sondeo incremental: se piden los eventos modificados desde aquí
    /// (<c>updatedMin</c>). Null obliga a una pasada completa de la ventana.
    /// </summary>
    public DateTime? PullCutoffAt { get; set; }

    /// <summary>Zona en la que se interpretan WorkDate + StartTime/EndTime (IANA o Windows).</summary>
    public string TimeZoneId { get; set; } = "America/Bogota";

    public bool SyncEnabled { get; set; } = true;
    public bool SyncTimedTasks { get; set; } = true;
    public bool SyncAllDayTasks { get; set; } = true;
    public bool SyncTimedNotes { get; set; } = true;

    /// <summary>Ventana que se empuja y se trae: días hacia atrás y hacia adelante de hoy.</summary>
    public int PastDays { get; set; } = 30;
    public int FutureDays { get; set; } = 180;

    public DateTime? LastSyncAt { get; set; }
    /// <summary>running | ok | error</summary>
    public string? LastSyncStatus { get; set; }
    public string? LastSyncMessage { get; set; }
    public int LastPushedCount { get; set; }
    public int LastPulledCount { get; set; }

    public DateTime ConnectedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
