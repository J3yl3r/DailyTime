namespace dailyTimeApi.Models.Triage;

/// <summary>
/// Reglas de descarte automático y umbrales de prioridad. Se guardan como JSON en
/// <see cref="dailyTimeApi.Models.Entities.OfferTriageConfig"/>; las propiedades ausentes toman estos valores.
/// </summary>
public class OfferTriageSettings
{
    /// <summary>Interruptor general del descarte automático (el puntaje se calcula igual).</summary>
    public bool AutoDiscardEnabled { get; set; } = true;

    /// <summary>Descarta ofertas publicadas hace más de N días. 0 = sin límite.</summary>
    public int MaxAgeDays { get; set; } = 30;

    /// <summary>Países aceptados. Vacío = los países preferidos del perfil (tu país siempre cuenta).</summary>
    public List<string> AllowedCountries { get; set; } = [];

    /// <summary>Descarta presencial o híbrido en un país distinto al tuyo.</summary>
    public bool DiscardOnsiteAbroad { get; set; } = true;

    /// <summary>Descarta también el presencial en tu país (para quien solo busca remoto o híbrido).</summary>
    public bool DiscardOnsiteLocal { get; set; } = true;

    /// <summary>
    /// Descarta ofertas que exigen residir en otro país ("Mexico only", "residentes en España") o cuyo título
    /// nombra un país que no aceptas.
    /// </summary>
    public bool DiscardResidencyAbroad { get; set; } = true;

    /// <summary>Descarta ofertas sin ninguno de tus stacks y cuyo título no es de un rol técnico.</summary>
    public bool DiscardOutOfProfile { get; set; } = true;

    /// <summary>Descarta la misma oferta (título y empresa) capturada en otro portal; conserva la primera.</summary>
    public bool DiscardDuplicates { get; set; } = true;

    /// <summary>
    /// Palabras que descartan si aparecen en el título (el tipo de contrato de los portales no es fiable).
    /// Si la palabra es una tecnología (p. ej. "java") y el título menciona uno de tus stacks, no descarta.
    /// </summary>
    public List<string> ExcludedTitleKeywords { get; set; } =
    [
        "practicante", "pasante", "aprendiz", "becario", "practicas", "intern", "internship",
        "curso", "diplomado", "bootcamp",
        "sap", "abap", "php", "cobol", "salesforce", "java"
    ];

    /// <summary>Empresas que nunca te interesan (coincidencia por palabra completa).</summary>
    public List<string> BlockedCompanies { get; set; } = [];

    /// <summary>Resta puntos si la oferta pide inglés avanzado y tu nivel en el perfil no lo es.</summary>
    public bool PenalizeEnglishGap { get; set; } = true;

    /// <summary>Analiza con IA (Gemini) las ofertas A y B activas que aún no tienen análisis.</summary>
    public bool AiEnabled { get; set; } = true;

    /// <summary>Máximo de análisis por día (la capa gratuita de Gemini tiene cuota diaria).</summary>
    public int AiDailyLimit { get; set; } = 40;

    /// <summary>Modelo de Gemini para el análisis.</summary>
    public string AiModel { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>Puntaje mínimo para prioridad A.</summary>
    public int TierAMin { get; set; } = 70;

    /// <summary>Puntaje mínimo para prioridad B (debajo es C).</summary>
    public int TierBMin { get; set; } = 45;
}
