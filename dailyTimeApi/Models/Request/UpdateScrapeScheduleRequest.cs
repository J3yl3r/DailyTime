namespace dailyTimeApi.Models.Request;

public class UpdateScrapeScheduleRequest
{
    public bool Enabled { get; set; }
    /// <summary>Horas locales "HH:mm".</summary>
    public List<string>? Times { get; set; }
    /// <summary>Días ISO (1 = lunes … 7 = domingo). Vacío o null = todos los días.</summary>
    public List<int>? Days { get; set; }
}

/// <summary>Registro de una franja atendida por el worker (en curso, terminada u omitida).</summary>
public class MarkScrapeSlotRequest
{
    public DateTime SlotAt { get; set; }
    /// <summary>running | completed | failed | cancelled | skipped</summary>
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class SetJobPortalAutoScrapeRequest
{
    public bool Enabled { get; set; }
}
