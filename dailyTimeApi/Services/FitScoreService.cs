using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class FitScoreService : IFitScoreService
{
    private static readonly string[] DotNetSignals =
    [
        ".net", "dotnet", "asp.net", "aspnet", "c#", "csharp", "net core", "net framework",
        "entity framework", "ef core", "blazor", "signalr", "azure", "sql server", "mssql"
    ];

    private static readonly string[] FrontendSignals =
    [
        "react", "typescript", "javascript", "next.js", "nextjs", "redux", "zustand", "tailwind",
        "frontend", "front-end"
    ];

    private static readonly string[] CloudSignals =
    [
        "azure", "aws", "gcp", "devops", "ci/cd", "docker", "kubernetes", "app service", "functions"
    ];

    private static readonly string[] RoleSignals =
    [
        "desarrollador", "developer", "programador", "ingeniero", "engineer", "backend", "full stack",
        "fullstack", "full-stack", "lider", "líder", "lead", "senior", "semi senior", "ssr", "mid",
        "consultor", "consultant"
    ];

    private readonly IJobApplicationRepository _applications;
    private readonly IWorkExperienceRepository _experiences;
    private readonly IJobOfferRepository _offers;

    public FitScoreService(
        IJobApplicationRepository applications,
        IWorkExperienceRepository experiences,
        IJobOfferRepository offers)
    {
        _applications = applications;
        _experiences = experiences;
        _offers = offers;
    }

    public async Task<FitScoreResponse> EvaluateAsync(
        EvaluateFitRequest request,
        CancellationToken cancellationToken = default)
    {
        string? offerUrl = request.OfferUrl;
        int? jobOfferId = request.JobOfferId;

        if (request.JobApplicationId is int applicationId)
        {
            var application = await _applications.GetByIdAsync(applicationId, cancellationToken)
                ?? throw new NotFoundException($"Postulación {applicationId} no encontrada.");

            if (string.IsNullOrWhiteSpace(offerUrl) && !string.IsNullOrWhiteSpace(application.Url))
                offerUrl = application.Url;
        }

        // Perfil completo = todas las experiencias (CV actualizado).
        // Solo si piden WorkExperienceId explícito se evalúa una sola.
        IReadOnlyList<WorkExperience> profile;
        if (request.WorkExperienceId is int singleId)
        {
            var one = await _experiences.GetByIdAsync(singleId, cancellationToken)
                ?? throw new NotFoundException($"Experiencia laboral {singleId} no encontrada.");
            profile = [one];
        }
        else
        {
            profile = await _experiences.GetAllAsync(cancellationToken);
            if (profile.Count == 0)
            {
                throw new ValidationException(
                    "No hay experiencias en tu perfil. Carga tu historial en Experiencias (o el seed SQL del CV) antes de evaluar.");
            }
        }

        JobOffer? offer = null;
        if (jobOfferId is int oid)
        {
            offer = await _offers.GetByIdAsync(oid, cancellationToken)
                ?? throw new NotFoundException($"Oferta {oid} no encontrada.");
        }
        else if (!string.IsNullOrWhiteSpace(offerUrl))
        {
            offer = await _offers.FindByUrlAsync(offerUrl.Trim(), cancellationToken);
        }

        var labels = profile
            .OrderByDescending(x => x.IsCurrent)
            .ThenByDescending(x => x.StartDate)
            .Select(x => $"{x.Position.Name} · {x.Company.Name}")
            .ToList();

        if (offer is null)
        {
            return new FitScoreResponse
            {
                Score = 0,
                Verdict = "maybe",
                VerdictLabel = "Sin oferta vinculada",
                Reasons =
                [
                    "No se encontró una oferta capturada con esa URL.",
                    "La postulación debe compartir URL con una oferta del scraper para comparar el texto de la vacante."
                ],
                Recommendations =
                [
                    "Abre la oferta desde Ofertas (scraping) y postula desde ahí, o pega en la postulación la misma URL capturada.",
                    "Con el perfil cargado ya puedes reintentar Evaluar encaje."
                ],
                SuggestedCv = BuildSuggestedCv(profile, offerText: null, matchedTechs: [], missingSignals: []),
                ProfileExperienceLabels = labels,
                ExperiencesUsed = profile.Count,
                WorkExperienceLabel = $"Perfil completo ({profile.Count} experiencia(s))",
                OfferFound = false
            };
        }

        return ScoreProfile(profile, offer, labels);
    }

    private static FitScoreResponse ScoreProfile(
        IReadOnlyList<WorkExperience> profile,
        JobOffer offer,
        IReadOnlyList<string> labels)
    {
        var offerText = Normalize(string.Join(' ',
            offer.Title,
            offer.Company,
            offer.Location,
            offer.Country,
            offer.WorkModality,
            offer.ContractType,
            offer.Description ?? offer.DescriptionSnippet,
            offer.Language));

        var techNames = profile
            .SelectMany(e => e.Technologies.Select(t => t.Technology?.Name ?? ""))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var experienceText = Normalize(string.Join(' ',
            profile.SelectMany(e => new[]
            {
                e.Position.Name,
                e.Company.Name,
                e.Field?.Name,
                e.Location?.Name,
                e.Summary,
                e.Achievements
            }),
            string.Join(' ', techNames)));

        var reasons = new List<string>();
        var recommendations = new List<string>();
        var missingSignals = new List<string>();
        var score = 15; // base: perfil con historial

        if (profile.Count >= 2)
        {
            score += 5;
            reasons.Add($"Se usó tu perfil completo ({profile.Count} experiencias), no una sola fila.");
        }

        var matchedTechs = techNames
            .Where(t => offerText.Contains(Normalize(t), StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matchedTechs.Count > 0)
        {
            score += Math.Min(40, matchedTechs.Count * 7);
            reasons.Add($"Tecnologías de tu historial presentes en la vacante: {string.Join(", ", matchedTechs.Take(10))}.");
        }
        else
        {
            reasons.Add("Pocas o ninguna tecnología de tu historial aparece literalmente en el texto de la oferta.");
            recommendations.Add("En el CV para esta vacante, nombra explícitamente el stack del anuncio (aunque ya lo hayas usado en proyectos).");
        }

        var dotNetOffer = ContainsAny(offerText, DotNetSignals);
        var dotNetCv = ContainsAny(experienceText, DotNetSignals) ||
                       techNames.Any(t => ContainsAny(Normalize(t), DotNetSignals));
        if (dotNetOffer && dotNetCv)
        {
            score += 15;
            reasons.Add("La vacante pide .NET/C#/Azure/SQL y tu historial lo cubre.");
            recommendations.Add("Pon arriba del CV: .NET + C# + SQL Server/Azure y el logro más cercano a lo que pide el anuncio.");
        }
        else if (dotNetOffer && !dotNetCv)
        {
            score -= 12;
            missingSignals.Add(".NET / C#");
            reasons.Add("La vacante enfatiza .NET/C# y tu perfil no lo refleja con claridad.");
            recommendations.Add("Si tienes experiencia .NET, agrégala a Experiencias/tecnologías; si no, esta vacante probablemente no conviene.");
        }

        var feOffer = ContainsAny(offerText, FrontendSignals);
        var feCv = ContainsAny(experienceText, FrontendSignals);
        if (feOffer && feCv)
        {
            score += 10;
            reasons.Add("Hay señal frontend (React/TS/etc.) en vacante y en tu historial.");
            recommendations.Add("Destaca React/TypeScript y un ejemplo de UI + estado (Zustand/Redux/Query) alineado al anuncio.");
        }
        else if (feOffer && !feCv)
        {
            missingSignals.Add("Frontend (React/TS)");
            recommendations.Add("La vacante pide frontend: resalta o agrega proyectos React/TypeScript en el CV enviado.");
        }

        var cloudOffer = ContainsAny(offerText, CloudSignals);
        var cloudCv = ContainsAny(experienceText, CloudSignals);
        if (cloudOffer && cloudCv)
        {
            score += 8;
            reasons.Add("Señal cloud/DevOps alineada (p. ej. Azure).");
            recommendations.Add("Menciona despliegues Azure/DevOps/CI-CD en el resumen y en Intelecto/Siempre Net.");
        }
        else if (cloudOffer && !cloudCv)
        {
            missingSignals.Add("Cloud / DevOps");
            recommendations.Add("Si aplicaste Azure/CI-CD, escríbelo; la vacante lo menciona y hoy no pesa en tu perfil.");
        }

        if (ContainsAny(offerText, RoleSignals) && ContainsAny(experienceText, RoleSignals))
        {
            score += 7;
            reasons.Add("El tipo de rol (desarrollador/consultor/lead) es compatible con tu trayectoria.");
        }

        // Ranking de experiencias más relevantes para esta vacante
        var ranked = profile
            .Select(e => new
            {
                Exp = e,
                Label = $"{e.Position.Name} · {e.Company.Name}",
                Hits = CountHits(e, offerText)
            })
            .OrderByDescending(x => x.Hits)
            .ThenByDescending(x => x.Exp.IsCurrent)
            .ThenByDescending(x => x.Exp.StartDate)
            .ToList();

        if (ranked[0].Hits > 0)
        {
            score += 5;
            reasons.Add($"Experiencia más alineada a esta vacante: {ranked[0].Label}.");
            recommendations.Add($"En el CV para esta vacante, abre con {ranked[0].Label} y 3–4 bullets del anuncio (no listes todo el historial con el mismo peso).");
        }

        var modality = Normalize(offer.WorkModality ?? "");
        if (modality.Contains("remoto", StringComparison.Ordinal) ||
            modality.Contains("hibrido", StringComparison.Ordinal))
        {
            score += 5;
            reasons.Add($"Modalidad: {offer.WorkModality}.");
        }

        if (!string.IsNullOrWhiteSpace(offer.Country))
        {
            var anyColombia = profile.Any(e =>
                Normalize(e.Location?.Name ?? "").Contains("colombia", StringComparison.Ordinal));
            var country = Normalize(offer.Country);
            if (country.Contains("colombia", StringComparison.Ordinal) && anyColombia)
            {
                score += 5;
                reasons.Add($"País alineado ({offer.Country}).");
            }
            else if (!country.Contains("colombia", StringComparison.Ordinal))
            {
                recommendations.Add($"Vacante en {offer.Country}: deja claro en el CV/carta que buscas remoto desde Colombia y zona horaria.");
            }
        }

        if (missingSignals.Count > 0)
            recommendations.Add($"Gaps detectados vs el anuncio: {string.Join(", ", missingSignals)}.");

        if (score >= 70)
            recommendations.Insert(0, "Encaje alto: vale la pena postular con un CV recortado a esta vacante (no el genérico).");
        else if (score >= 40)
            recommendations.Insert(0, "Encaje medio: postula solo si puedes enfatizar los puntos fuertes del borrador sugerido abajo.");
        else
            recommendations.Insert(0, "Encaje bajo: prioriza otras vacantes o refuerza el perfil antes de invertir tiempo.");

        score = Math.Clamp(score, 0, 100);

        string verdict;
        string label;
        if (score >= 70)
        {
            verdict = "yes";
            label = "Sí — conviene postular con CV enfocado";
        }
        else if (score >= 40)
        {
            verdict = "maybe";
            label = "Dudoso — solo con CV adaptado";
        }
        else
        {
            verdict = "no";
            label = "Bajo — mejor otra vacante";
        }

        reasons.Insert(0, $"Score {score}/100 · {label}.");

        return new FitScoreResponse
        {
            Score = score,
            Verdict = verdict,
            VerdictLabel = label,
            Reasons = reasons,
            Recommendations = recommendations.Distinct().ToList(),
            SuggestedCv = BuildSuggestedCv(profile, offerText, matchedTechs, missingSignals, ranked.Select(r => (r.Label, r.Hits, r.Exp)).ToList(), offer),
            ProfileExperienceLabels = labels,
            ExperiencesUsed = profile.Count,
            WorkExperienceLabel = $"Perfil completo ({profile.Count} experiencia(s))",
            JobOfferId = offer.Id,
            JobOfferTitle = offer.Title,
            OfferFound = true
        };
    }

    private static int CountHits(WorkExperience e, string offerText)
    {
        var blob = Normalize(string.Join(' ',
            e.Position.Name,
            e.Summary,
            e.Achievements,
            string.Join(' ', e.Technologies.Select(t => t.Technology?.Name))));
        if (string.IsNullOrWhiteSpace(blob))
            return 0;

        var tokens = offerText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 4)
            .Distinct()
            .Take(80);
        return tokens.Count(t => blob.Contains(t, StringComparison.Ordinal));
    }

    private static string BuildSuggestedCv(
        IReadOnlyList<WorkExperience> profile,
        string? offerText,
        IReadOnlyList<string> matchedTechs,
        IReadOnlyList<string> missingSignals,
        List<(string Label, int Hits, WorkExperience Exp)>? ranked = null,
        JobOffer? offer = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CV sugerido para esta vacante (énfasis, no archivo nuevo)");
        sb.AppendLine("================================================");
        if (offer != null)
            sb.AppendLine($"Objetivo: {offer.Title} · {offer.Company ?? "Empresa"}");

        sb.AppendLine();
        sb.AppendLine("1) Resumen (adapta 2–3 líneas):");
        sb.AppendLine("Desarrollador Full-Stack (.NET / React / SQL / Azure) con experiencia en");
        sb.AppendLine("aplicaciones web escalables, migraciones y despliegues en la nube.");
        if (matchedTechs.Count > 0)
            sb.AppendLine($"Stack a mencionar primero: {string.Join(", ", matchedTechs.Take(8))}.");

        sb.AppendLine();
        sb.AppendLine("2) Orden recomendado de experiencias (más relevante arriba):");
        var order = ranked?.Select(r => r.Exp).ToList()
                    ?? profile.OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.StartDate).ToList();
        var i = 1;
        foreach (var e in order)
        {
            var end = e.IsCurrent ? "Actualidad" : e.EndDate?.ToString("yyyy-MM") ?? "?";
            sb.AppendLine($"   {i}. {e.Position.Name} · {e.Company.Name} ({e.StartDate:yyyy-MM} – {end})");
            if (!string.IsNullOrWhiteSpace(e.Achievements))
            {
                var bullets = e.Achievements.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(3);
                foreach (var b in bullets)
                    sb.AppendLine($"      {b.TrimStart('-', ' ')}");
            }
            i++;
        }

        sb.AppendLine();
        sb.AppendLine("3) Qué enfatizar / qué recortar:");
        sb.AppendLine("   - Enfatiza bullets que usen el mismo lenguaje del anuncio (tecnologías y resultados).");
        sb.AppendLine("   - Recorta tareas genéricas que no aporten al stack de la vacante.");
        if (missingSignals.Count > 0)
            sb.AppendLine($"   - Si aplica, añade evidencia de: {string.Join(", ", missingSignals)}.");

        if (!string.IsNullOrWhiteSpace(offerText) && offerText.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("4) Checklist antes de enviar:");
            sb.AppendLine("   - Título/headline alineado al puesto del anuncio.");
            sb.AppendLine("   - 1 logro medible arriba (migración, performance, Azure, etc.).");
            sb.AppendLine("   - Keywords del anuncio visibles en la primera mitad de página.");
        }

        return sb.ToString().Trim();
    }

    private static bool ContainsAny(string haystack, IEnumerable<string> needles) =>
        needles.Any(n => haystack.Contains(Normalize(n), StringComparison.Ordinal));

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var formD = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(c);
        }

        var text = sb.ToString().Normalize(NormalizationForm.FormC);
        text = text.Replace("c#", "csharp", StringComparison.Ordinal);
        text = text.Replace(".net", "dotnet", StringComparison.Ordinal);
        text = Regex.Replace(text, @"\s+", " ");
        return text;
    }
}
