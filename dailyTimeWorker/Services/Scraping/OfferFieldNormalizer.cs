using System.Text.RegularExpressions;

namespace dailyTimeWorker.Services.Scraping;

/// <summary>
/// Normaliza modalidad/contrato a catálogos fijos.
/// Si el texto no encaja en un valor conocido, devuelve null (nunca texto basura).
/// </summary>
public static class OfferFieldNormalizer
{
    /// <summary>Valores canónicos de modalidad de trabajo.</summary>
    public static readonly string[] AllowedModalities =
    [
        "Remoto",
        "Híbrido",
        "Presencial"
    ];

    /// <summary>Valores canónicos de tipo de contrato (no jornada).</summary>
    public static readonly string[] AllowedContractTypes =
    [
        "Indefinido",
        "Término fijo",
        "Obra o labor",
        "Prestación de servicios",
        "Temporal",
        "Prácticas",
        "Freelance"
    ];

    private static readonly string[] DefaultExcludeKeywords =
    [
        "discapacitad",
        "discapacidad",
        "persona con discapacidad",
        "personas con discapacidad",
        "inclusión laboral",
        "inclusion laboral",
        "programa de inclusión",
        "programa de inclusion",
        "cupo de discapacidad",
        "ley de discapacidad",
        "diversidad e inclusión",
        "diversidad e inclusion",
        "pcd",
        "silla de ruedas",
        "accesibilidad exclusiva",
        "solo para personas con discapacidad",
        "vacante inclusiva",
        "empleo inclusivo exclusivo"
    ];

    public static bool MatchesExclusion(string? text, IReadOnlyList<string>? extraKeywords = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var haystack = text.ToLowerInvariant();
        foreach (var keyword in DefaultExcludeKeywords)
        {
            if (haystack.Contains(keyword, StringComparison.Ordinal))
                return true;
        }

        if (extraKeywords is null)
            return false;

        foreach (var keyword in extraKeywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;
            if (haystack.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// True si el texto contiene al menos una señal de inclusión (stack .NET, etc.).
    /// Normaliza variantes (c#/csharp, .net/dotnet) y evita el “net” suelto sin contexto.
    /// </summary>
    public static bool MatchesInclusion(string? text, IReadOnlyList<string>? requireKeywords)
    {
        if (requireKeywords is null || requireKeywords.Count == 0)
            return true;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var haystack = NormalizeTechHaystack(text);
        foreach (var keyword in requireKeywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                continue;
            if (MatchesTechKeyword(haystack, keyword.Trim()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// La vacante es válida si el stack requerido aparece en el título O en el contenido
    /// (snippet/descripción). Se evalúan por separado para no perder señales en uno u otro.
    /// </summary>
    public static bool MatchesRequiredTech(
        string? title,
        string? content,
        IReadOnlyList<string>? requireKeywords)
    {
        if (requireKeywords is null || requireKeywords.Count == 0)
            return true;

        return MatchesInclusion(title, requireKeywords)
            || MatchesInclusion(content, requireKeywords);
    }

    private static string NormalizeTechHaystack(string text)
    {
        var t = text.ToLowerInvariant();
        // Unificar puntos unicode raros antes de detectar .net
        t = t.Replace('\u2024', '.').Replace('\uFF0E', '.').Replace('\u00B7', '.');
        // Orden importa: asp.net antes que .net.
        t = t.Replace("asp.net", " aspnet ", StringComparison.Ordinal);
        t = t.Replace("next.js", " nextjs ", StringComparison.Ordinal);
        t = t.Replace("next js", " nextjs ", StringComparison.Ordinal);
        t = t.Replace("c#", " csharp ", StringComparison.Ordinal);
        t = t.Replace("c♯", " csharp ", StringComparison.Ordinal);
        t = t.Replace(".net", " dotnet ", StringComparison.Ordinal);
        // "NET" / "Net" suelto (títulos tipo "Desarrollador NET Core") sin convertir "internet".
        t = Regex.Replace(t, @"(?<![a-z0-9])net(?![a-z])", " dotnet ");
        t = Regex.Replace(t, @"\s+", " ");
        return t;
    }

    /// <summary>
    /// Detecta stacks principales para filtrar ofertas (.NET, React, Next.js, Java, JS, …).
    /// </summary>
    public static string? DetectTechStack(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var hay = NormalizeTechHaystack(text);
        var stacks = new List<string>();

        if (hay.Contains("dotnet", StringComparison.Ordinal)
            || hay.Contains("csharp", StringComparison.Ordinal)
            || hay.Contains("aspnet", StringComparison.Ordinal)
            || hay.Contains("entity framework", StringComparison.Ordinal)
            || hay.Contains("ef core", StringComparison.Ordinal))
            stacks.Add(".NET");

        if (hay.Contains("nextjs", StringComparison.Ordinal))
            stacks.Add("Next.js");

        if (Regex.IsMatch(hay, @"\breact\b") || hay.Contains("react.js", StringComparison.Ordinal)
            || hay.Contains("reactjs", StringComparison.Ordinal))
            stacks.Add("React");

        if (hay.Contains("typescript", StringComparison.Ordinal)
            || Regex.IsMatch(hay, @"\bjavascript\b")
            || Regex.IsMatch(hay, @"(?<![a-z])js(?![a-z])")
            || hay.Contains("node.js", StringComparison.Ordinal)
            || hay.Contains("nodejs", StringComparison.Ordinal))
            stacks.Add("JavaScript");

        // Java sin confundir con JavaScript.
        if (Regex.IsMatch(hay, @"\bjava\b") && !hay.Contains("javascript", StringComparison.Ordinal))
            stacks.Add("Java");

        if (Regex.IsMatch(hay, @"\bpython\b") || hay.Contains("django", StringComparison.Ordinal)
            || hay.Contains("fastapi", StringComparison.Ordinal))
            stacks.Add("Python");

        if (Regex.IsMatch(hay, @"\bangular\b"))
            stacks.Add("Angular");

        if (Regex.IsMatch(hay, @"\bvue\b") || hay.Contains("vue.js", StringComparison.Ordinal)
            || hay.Contains("nuxt", StringComparison.Ordinal))
            stacks.Add("Vue");

        return stacks.Count == 0 ? null : string.Join(", ", stacks.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static bool MatchesTechKeyword(string haystack, string keyword)
    {
        var k = keyword.Trim().ToLowerInvariant();
        if (k.Length == 0)
            return false;

        // Alias → tokens ya normalizados en el haystack.
        k = k switch
        {
            "c#" or "c♯" => "csharp",
            ".net" => "dotnet",
            "asp.net" => "aspnet",
            ".net core" or "net core" => "dotnet core",
            "next.js" or "next js" => "nextjs",
            "react.js" or "reactjs" => "react",
            _ => k
        };

        // "net" solo: demasiado ambiguo (internet, etc.).
        if (k is "net")
            return false;

        if (haystack.Contains(k, StringComparison.Ordinal))
            return true;

        // Variante sin espacios: "efcore", "aspnetcore"
        var compact = k.Replace(" ", "", StringComparison.Ordinal);
        if (compact.Length >= 4 && haystack.Replace(" ", "", StringComparison.Ordinal)
                .Contains(compact, StringComparison.Ordinal))
            return true;

        return false;
    }

    public static string? NormalizeModality(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = NormalizeForMatch(raw);

        // Híbrido primero: a menudo aparece junto a "remoto" / "presencial".
        if (ContainsAny(text, "híbrido", "hibrido", "hybrid", "semi-presencial", "semipresencial",
                "semi presencial", "parcialmente remoto", "remoto parcial",
                "presencial y remoto", "remoto y presencial", "presencial/remoto", "remoto/presencial"))
            return "Híbrido";

        if (ContainsAny(text, "100% remoto", "fully remote", "remote", "remoto", "teletrabajo",
                "work from home", "wfh", "desde casa", "home office", "trabajo remoto"))
            return "Remoto";

        if (ContainsAny(text, "presencial", "on-site", "onsite", "on site", "en oficina",
                "en sitio", "oficina"))
            return "Presencial";

        return null;
    }

    public static string? NormalizeContractType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = NormalizeForMatch(raw);

        // Prestación de servicios (contrato civil) — no confundir con "prestaciones sociales".
        if (ContainsAny(text, "prestación de servicios", "prestacion de servicios",
                "contrato por prestación", "contrato por prestacion", "por prestación",
                "por prestacion"))
            return "Prestación de servicios";

        if (ContainsAny(text, "obra o labor", "obra labor", "por obra"))
            return "Obra o labor";

        if (ContainsAny(text, "término fijo", "termino fijo", "contrato fijo", "plazo fijo",
                "a término fijo", "a termino fijo"))
            return "Término fijo";

        if (ContainsAny(text, "indefinido", "término indefinido", "termino indefinido",
                "contrato indefinido", "permanente", "permanent"))
            return "Indefinido";

        if (ContainsAny(text, "práctica", "practica", "pasantía", "pasantia", "internship",
                "becario", "trainee"))
            return "Prácticas";

        if (ContainsAny(text, "freelance", "independiente", "contractor", "consultor externo"))
            return "Freelance";

        if (ContainsAny(text, "temporal", "determinado", "por temporada"))
            return "Temporal";

        // Jornada (full/part time) NO es tipo de contrato → se ignora a propósito.
        return null;
    }

    public static string? GuessModalityFromText(string? text) =>
        NormalizeModality(text);

    public static string? GuessContractFromText(string? text) =>
        NormalizeContractType(text);

    private static string NormalizeForMatch(string raw) =>
        Regex.Replace(raw.Trim().ToLowerInvariant(), @"\s+", " ");

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
