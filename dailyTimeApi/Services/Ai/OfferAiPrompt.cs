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
        Requisitos obligatorios: solo los que la oferta exige, no los deseables. Para cada uno usa
        "yes" si el perfil del candidato lo respalda; "no" solo si el perfil muestra claramente que no lo
        cumple (por ejemplo, otro stack o un nivel de inglés menor); "unknown" si el perfil no tiene ese dato
        (por ejemplo, años de experiencia no registrados). Un dato ausente del perfil nunca es "no".
        missingMustHaves: solo requisitos marcados "no"; nunca los "unknown".
        locationRestriction: solo una restricción de residencia o ubicación que excluya al candidato por su
        país de residencia; déjalo vacío si la oferta lo admite o no hay restricción.
        Veredicto: "apply" si cumple lo esencial que se puede verificar; "maybe" si hay dudas o datos
        desconocidos; "skip" solo si le faltan requisitos obligatorios importantes marcados "no" o una
        restricción lo excluye (residencia, nivel de idioma o presencialidad fuera de su país). La falta de
        datos en el perfil nunca justifica "skip" por sí sola.
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
            "seniority": { "type": "string", "enum": ["junior", "semi-senior", "senior", "lead", "unknown"] },
            "requiredYears": { "type": "integer", "minimum": 0, "maximum": 30 },
            "englishLevel": { "type": "string", "enum": ["none", "basic", "intermediate", "advanced", "unknown"] },
            "workModality": { "type": "string", "enum": ["remote", "hybrid", "onsite", "unknown"] },
            "locationRestriction": { "type": "string", "description": "Restricción de residencia o ubicación que excluye al candidato por su país; vacío si lo admite o no hay." },
            "missingMustHaves": { "type": "array", "maxItems": 5, "items": { "type": "string" }, "description": "Solo requisitos obligatorios marcados como no cumplidos." },
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
