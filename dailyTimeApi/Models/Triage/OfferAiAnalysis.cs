namespace dailyTimeApi.Models.Triage;

/// <summary>Análisis de una oferta hecho con IA. Se guarda como JSON en <c>JobOffer.AiAnalysis</c>.</summary>
public class OfferAiAnalysis
{
    /// <summary>apply | maybe | skip</summary>
    public string Verdict { get; set; } = "maybe";

    /// <summary>Resumen breve del encaje, en español.</summary>
    public string Summary { get; set; } = string.Empty;

    public List<OfferAiRequirement> MandatoryRequirements { get; set; } = [];

    /// <summary>Requisitos obligatorios que el perfil no respalda.</summary>
    public List<string> MissingMustHaves { get; set; } = [];

    /// <summary>junior | semi-senior | senior | lead | unknown</summary>
    public string Seniority { get; set; } = "unknown";

    /// <summary>Años de experiencia que pide la oferta; 0 = no indicado.</summary>
    public int RequiredYears { get; set; }

    /// <summary>none | basic | intermediate | advanced | unknown</summary>
    public string EnglishLevel { get; set; } = "unknown";

    /// <summary>remote | hybrid | onsite | unknown</summary>
    public string WorkModality { get; set; } = "unknown";

    /// <summary>Restricción de residencia o ubicación; vacío si no hay.</summary>
    public string LocationRestriction { get; set; } = string.Empty;

    public OfferAiSalary Salary { get; set; } = new();

    /// <summary>Alertas: condiciones dudosas, requisitos excesivos, etc.</summary>
    public List<string> RedFlags { get; set; } = [];
}

public class OfferAiRequirement
{
    public string Requirement { get; set; } = string.Empty;

    /// <summary>yes | partial | no | unknown</summary>
    public string Met { get; set; } = "unknown";
}

public class OfferAiSalary
{
    /// <summary>0 = no indicado.</summary>
    public decimal Min { get; set; }

    /// <summary>0 = no indicado.</summary>
    public decimal Max { get; set; }

    /// <summary>Código de moneda (COP, USD, EUR, MXN…) o vacío.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>month | year | hour | unknown</summary>
    public string Period { get; set; } = "unknown";
}
