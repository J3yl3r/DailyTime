namespace dailyTimeApi.Models.Entities;

/// <summary>
/// Lápida de un elemento borrado en DailyTime que ya tenía evento en Google. La
/// sincronización borra el evento remoto y luego elimina la lápida; así el borrado no
/// depende de que Google responda dentro del request del usuario.
/// </summary>
public class GoogleSyncDeletion
{
    public int Id { get; set; }
    public string GoogleEventId { get; set; } = string.Empty;
    public string CalendarId { get; set; } = "primary";
    public DateTime DeletedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
