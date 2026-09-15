using System.Globalization;
using System.Text;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Services.Ai;

/// <summary>
/// Prompt y esquema de salida del análisis. Del perfil solo se envían país, países aceptados, modalidad
/// preferida, stacks, años e inglés: nunca nombre, correo, teléfono ni salario.
/// </summary>
public static class OfferAiPrompt
{
    public const int MaxDescriptionChars = 6000;

    public const string SystemInstruction = """
        Eres un asistente que evalúa ofertas de empleo para un candidato de desarrollo de software.
        Responde únicamente con el JSON del esquema, con textos en español, claros y breves.
        No inventes datos: si algo no aparece en la oferta usa "unknown", 0, "" o una lista vacía.
        Los requisitos obligatorios son los que la oferta exige, no los deseables. Márcalos como cumplidos
        solo si el perfil del candidato lo respalda.
        Veredicto: "apply" si el candidato cumple lo esencial; "maybe" si le faltan cosas menores o hay dudas;
        "skip" si le faltan requisitos obligatorios importantes o hay una restricción que lo excluye
        (residencia, nivel de idioma o presencialidad fuera de su país).
        """;

    public const string ResponseSchemaJson = """
        {
          "type": "object",
          "properties": {
            "verdict": { "type": "string", "enum": ["apply", "maybe", "skip"] },
            "summary": { "type": "string", "description": "Una o dos frases en español sobre el encaje del candidato." },
            "mandatoryRequirements": {
              "type": "array",
              "maxItems": 8,
              "items": {
                "type": "object",
                "properties": {
                  "requirement": { "type": "string" },
                  "met": { "type": "string", "enum": ["yes", "partial", "no", "unknown"] }
                },
                "required": ["requirement", "met"]
              }
            },
            "missingMustHaves": { "type": "array", "maxItems": 5, "items": { "type": "string" } },
            "seniority": { "type": "string", "enum": ["junior", "semi-senior", "senior", "lead", "unknown"] },
            "requiredYears": { "type": "integer", "minimum": 0, "maximum": 30 },
            "englishLevel": { "type": "string", "enum": ["none", "basic", "intermediate", "advanced", "unknown"] },
            "workModality": { "type": "string", "enum": ["remote", "hybrid", "onsite", "unknown"] },
            "locationRestriction": { "type": "string", "description": "Restricción de residencia o ubicación; vacío si no hay." },
            "salary": {
              "type": "object",
              "properties": {
                "min": { "type": "number" },
                "max": { "type": "number" },
                "currency": { "type": "string" },
                "period": { "type": "string", "enum": ["month", "year", "hour", "unknown"] }
              },
              "required": ["min", "max", "currency", "period"]
            },
            "redFlags": { "type": "array", "maxItems": 5, "items": { "type": "string" } }
          },
          "required": ["verdict", "summary", "mandatoryRequirements", "missingMustHaves", "seniority", "requiredYears", "englishLevel", "workModality", "locationRestriction", "salary", "redFlags"]
        }
        """;

    public static string BuildUserPrompt(JobOffer offer, TriageProfile profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PERFIL DEL CANDIDATO");
        sb.AppendLine($"- País de residencia: {profile.HomeCountry ?? "no indicado"}");
        sb.AppendLine($"- Países donde acepta trabajar en remoto: {JoinOrNone(profile.AllowedCountries)}");
        sb.AppendLine($"- Modalidad preferida: {profile.PreferredModality ?? "no indicada"}");
        sb.AppendLine($"- Stacks y tecnologías: {JoinOrNone(profile.Stacks)}");
        sb.AppendLine(profile.ExperienceYears > 0
            ? $"- Años de experiencia: {profile.ExperienceYears.ToString("0.#", CultureInfo.InvariantCulture)}"
            : "- Años de experiencia: no registrados");
        sb.AppendLine($"- Nivel de inglés: {profile.EnglishLevel ?? "no indicado"}");
        sb.AppendLine();
        sb.AppendLine("OFERTA");
        sb.AppendLine($"- Título: {offer.Title}");
        AppendIfPresent(sb, "Empresa", offer.Company);
        AppendIfPresent(sb, "Ubicación", offer.Location);
        AppendIfPresent(sb, "País", offer.Country);
        AppendIfPresent(sb, "Modalidad indicada por el portal", offer.WorkModality);
        sb.AppendLine("- Descripción:");
        var description = (offer.Description ?? offer.DescriptionSnippet ?? string.Empty).Trim();
        sb.AppendLine(description.Length == 0
            ? "(sin descripción)"
            : description.Length <= MaxDescriptionChars ? description : description[..MaxDescriptionChars] + "…");
        return sb.ToString();
    }

    private static void AppendIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine($"- {label}: {value.Trim()}");
    }

    private static string JoinOrNone(IReadOnlyList<string> values) =>
        values.Count == 0 ? "no indicados" : string.Join(", ", values);
}
