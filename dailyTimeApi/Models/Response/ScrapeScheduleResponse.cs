namespace dailyTimeApi.Models.Response;

public class ScrapeScheduleResponse
{
    public bool Enabled { get; set; }
    /// <summary>Horas locales "HH:mm" ordenadas.</summary>
    public IReadOnlyList<string> Times { get; set; } = [];
    /// <summary>Días ISO (1 = lunes … 7 = domingo). Vacío = todos los días.</summary>
    public IReadOnlyList<int> Days { get; set; } = [];
    public string TimeZoneId { get; set; } = string.Empty;
    public DateTime ConfigUpdatedAt { get; set; }
    public DateTime? LastSlotAt { get; set; }
    public string? LastSlotStatus { get; set; }
    public string? LastSlotMessage { get; set; }
    public DateTime? LastSlotFinishedAt { get; set; }
    /// <summary>
    /// Próxima franja a ejecutar (UTC). Puede estar unos minutos en el pasado (dentro de
    /// <see cref="LateToleranceMinutes"/>): en ese caso el worker debe ejecutarla ya.
    /// </summary>
    public DateTime? NextSlotAt { get; set; }
    /// <summary>Franja más reciente que se perdió (equipo apagado/ocupado) y aún no se registró como omitida.</summary>
    public DateTime? MissedSlotAt { get; set; }
    public int LateToleranceMinutes { get; set; }
    public DateTime ServerUtc { get; set; }
}
