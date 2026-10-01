namespace dailyTimeApi.Models.Response;

public class GoogleCalendarStatusResponse
{
    /// <summary>Hay Google:ClientId y Google:ClientSecret en user secrets.</summary>
    public bool Configured { get; set; }

    public bool Connected { get; set; }
    public string? Email { get; set; }
    public string? CalendarId { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;

    public bool SyncEnabled { get; set; }
    public bool SyncTimedTasks { get; set; }
    public bool SyncAllDayTasks { get; set; }
    public bool SyncTimedNotes { get; set; }
    public int PastDays { get; set; }
    public int FutureDays { get; set; }

    public DateTime? LastSyncAt { get; set; }
    /// <summary>running | ok | error</summary>
    public string? LastSyncStatus { get; set; }
    public string? LastSyncMessage { get; set; }
    public int LastPushedCount { get; set; }
    public int LastPulledCount { get; set; }
    public DateTime? ConnectedAt { get; set; }

    public bool IsRunning { get; set; }
    public int SyncIntervalMinutes { get; set; }

    /// <summary>URI de redirección que debe estar autorizada en Google Cloud.</summary>
    public string RedirectUri { get; set; } = string.Empty;
}

public class GoogleCalendarListItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public bool IsPrimary { get; set; }
}

public class GoogleSyncRunResponse
{
    public int Pushed { get; set; }
    public int Pulled { get; set; }
    public int Deleted { get; set; }
    public string? Message { get; set; }
}

public class GoogleAuthUrlResponse
{
    public string AuthUrl { get; set; } = string.Empty;
}
