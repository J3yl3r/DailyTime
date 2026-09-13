namespace dailyTimeApi.Models.Entities;

/// <summary>
/// Horario global de captura automática (una sola fila). A cada hora definida el worker
/// ejecuta, uno tras otro, los portales activos con <see cref="JobPortal.AutoScrapeEnabled"/>.
/// </summary>
public class ScrapeSchedule
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    /// <summary>Horas locales "HH:mm" separadas por coma, ej: "07:00,12:00,18:00".</summary>
    public string Times { get; set; } = string.Empty;
    /// <summary>Días ISO (1 = lunes … 7 = domingo) separados por coma. Vacío = todos los días.</summary>
    public string? Days { get; set; }
    /// <summary>Zona horaria en la que se interpretan las horas (IANA o Windows).</summary>
    public string TimeZoneId { get; set; } = "America/Bogota";
    /// <summary>Último cambio de horas/días/activación. Las horas anteriores a este momento no se ejecutan.</summary>
    public DateTime ConfigUpdatedAt { get; set; }
    /// <summary>Hora programada (UTC) de la última franja atendida (ejecutada, omitida o en curso).</summary>
    public DateTime? LastSlotAt { get; set; }
    /// <summary>running | completed | failed | cancelled | skipped</summary>
    public string? LastSlotStatus { get; set; }
    public string? LastSlotMessage { get; set; }
    /// <summary>Fin real (UTC) de la última ejecución. Las horas que caen durante una ejecución se omiten.</summary>
    public DateTime? LastSlotFinishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
