namespace dailyTimeApi.Models.Response;

public class OfferAiStatusResponse
{
    /// <summary>Hay clave de Gemini configurada.</summary>
    public bool Configured { get; set; }
    public bool Enabled { get; set; }
    public string Model { get; set; } = string.Empty;
    public int DailyLimit { get; set; }
    /// <summary>Análisis hechos desde la medianoche del Pacífico (incluye los fallidos).</summary>
    public int UsedToday { get; set; }
    /// <summary>Ofertas activas A/B sin análisis.</summary>
    public int Pending { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? PausedUntil { get; set; }
    public string? PauseReason { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastRunAt { get; set; }
    /// <summary>Próxima renovación de la cuota diaria.</summary>
    public DateTime ResetsAt { get; set; }
}
