namespace dailyTimeApi.Models.Entities;

/// <summary>Reglas de descarte y umbrales de prioridad de ofertas (una sola fila, guardada como JSON).</summary>
public class OfferTriageConfig
{
    public int Id { get; set; }
    /// <summary>JSON de <see cref="dailyTimeApi.Models.Triage.OfferTriageSettings"/>.</summary>
    public string SettingsJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}
