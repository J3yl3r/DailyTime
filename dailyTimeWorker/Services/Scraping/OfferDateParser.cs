using System.Globalization;
using System.Text.RegularExpressions;

namespace dailyTimeWorker.Services.Scraping;

/// <summary>
/// Interpreta textos de fecha relativos/absolutos de portales (elempleo, LinkedIn, Indeed…).
/// </summary>
public static class OfferDateParser
{
    private static readonly Regex RelativeEs = new(
        @"(?:publicado\s+|publicada\s+|posted\s+)?hace\s+(?:m[aá]s\s+de\s+)?(\d+)\s*(minuto|minutos|hora|horas|d[ií]a|d[ií]as|semana|semanas|mes|meses|a[nñ]o|a[nñ]os)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RelativeEn = new(
        @"(?:posted\s+|active\s+|hired\s+)?(\d+)\s*(minute|minutes|hour|hours|day|days|week|weeks|month|months|year|years)\s+ago",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RelativeEnCompact = new(
        @"(\d+)\s*\+\s*(day|days|week|weeks)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] AbsoluteFormats =
    [
        "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy",
        "yyyy-MM-dd", "d MMM yyyy", "dd MMM yyyy", "MMM d, yyyy", "MMMM d, yyyy"
    ];

    public static int? TryParseAgeDays(string? raw, DateTime? utcNow = null)
    {
        var hours = TryParseAgeHours(raw, utcNow);
        if (hours is null)
            return null;
        return (int)Math.Floor(hours.Value / 24.0);
    }

    /// <summary>
    /// Edad aproximada en horas. Útil para filtros finos (ej. LinkedIn ≤ 5h).
    /// </summary>
    public static double? TryParseAgeHours(string? raw, DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = string.Join(" ", raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (text.Length == 0)
            return null;

        var now = utcNow ?? DateTime.UtcNow;
        var lower = text.ToLowerInvariant();

        if (lower is "hoy" or "today" or "just now" or "ahora" or "recién" or "recien"
            or "nuevo" or "new")
            return 0;
        if (lower is "ayer" or "yesterday")
            return 24;
        if (lower.Contains("hoy y ayer", StringComparison.Ordinal))
            return 24;

        var es = RelativeEs.Match(text);
        if (es.Success && int.TryParse(es.Groups[1].Value, out var esQty))
            return ToHours(esQty, es.Groups[2].Value);

        var en = RelativeEn.Match(text);
        if (en.Success && int.TryParse(en.Groups[1].Value, out var enQty))
            return ToHours(enQty, en.Groups[2].Value);

        var enCompact = RelativeEnCompact.Match(text);
        if (enCompact.Success && int.TryParse(enCompact.Groups[1].Value, out var enCompactQty))
            return ToHours(enCompactQty, enCompact.Groups[2].Value);

        if (lower.Contains("hoy", StringComparison.Ordinal) && lower.Length < 40)
            return 0;
        if (lower.Contains("ayer", StringComparison.Ordinal) && lower.Length < 40)
            return 24;

        foreach (var format in AbsoluteFormats)
        {
            if (DateTime.TryParseExact(
                    text, format, CultureInfo.GetCultureInfo("es-CO"),
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedEs)
                || DateTime.TryParseExact(
                    text, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out parsedEs))
            {
                return Math.Max(0, (now - parsedEs.ToUniversalTime()).TotalHours);
            }
        }

        if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("es-CO"), DateTimeStyles.AssumeUniversal, out var loose)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out loose))
        {
            return Math.Max(0, (now - loose.ToUniversalTime()).TotalHours);
        }

        return null;
    }

    public static DateTime? TryParsePostedAtUtc(string? raw, DateTime? utcNow = null)
    {
        var hours = TryParseAgeHours(raw, utcNow);
        return hours is null ? null : (utcNow ?? DateTime.UtcNow).AddHours(-hours.Value);
    }

    private static double ToHours(int qty, string unit)
    {
        qty = Math.Max(0, qty);
        var u = unit.ToLowerInvariant();
        if (u.StartsWith("minuto", StringComparison.Ordinal) || u.StartsWith("minute", StringComparison.Ordinal))
            return qty / 60.0;
        if (u.StartsWith("hora", StringComparison.Ordinal) || u.StartsWith("hour", StringComparison.Ordinal))
            return qty;
        if (u.StartsWith("d", StringComparison.Ordinal))
            return qty * 24.0;
        if (u.StartsWith("semana", StringComparison.Ordinal) || u.StartsWith("week", StringComparison.Ordinal))
            return qty * 7 * 24.0;
        if (u.StartsWith("mes", StringComparison.Ordinal) || u.StartsWith("month", StringComparison.Ordinal))
            return qty * 30 * 24.0;
        if (u.StartsWith("a", StringComparison.Ordinal) || u.StartsWith("year", StringComparison.Ordinal))
            return qty * 365 * 24.0;
        return qty * 24.0;
    }
}
