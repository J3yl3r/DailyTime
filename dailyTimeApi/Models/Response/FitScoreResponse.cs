namespace dailyTimeApi.Models.Response;

public class FitScoreResponse
{
    public int Score { get; set; }
    /// <summary>yes | maybe | no</summary>
    public string Verdict { get; set; } = "maybe";
    public string VerdictLabel { get; set; } = string.Empty;
    /// <summary>Hallazgos del matching (qué cuadró / qué falta).</summary>
    public IReadOnlyList<string> Reasons { get; set; } = [];
    /// <summary>Acciones concretas para mejorar el encaje o la postulación.</summary>
    public IReadOnlyList<string> Recommendations { get; set; } = [];
    /// <summary>Borrador de CV orientado a esa vacante (énfasis por experiencia).</summary>
    public string? SuggestedCv { get; set; }
    public IReadOnlyList<string> ProfileExperienceLabels { get; set; } = [];
    public int ExperiencesUsed { get; set; }
    public int? WorkExperienceId { get; set; }
    public string? WorkExperienceLabel { get; set; }
    public int? JobOfferId { get; set; }
    public string? JobOfferTitle { get; set; }
    public bool OfferFound { get; set; }
}
