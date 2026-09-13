using System.Globalization;

namespace dailyTimeApi.Services;

/// <summary>
/// Cálculo de franjas del horario global de captura. Todas las entradas y salidas son UTC;
/// las horas y días se interpretan en la zona horaria del horario.
/// </summary>
public static class ScrapeSlotCalculator
{
    public const string DefaultTimeZoneId = "America/Bogota";

    public static IReadOnlyList<TimeOnly> ParseTimes(string? raw) =>
        (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => TimeOnly.TryParseExact(t, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
                ? time
                : (TimeOnly?)null)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .Distinct()
            .Order()
            .ToList();

    /// <summary>Días ISO (1 = lunes … 7 = domingo). Lista vacía = todos los días.</summary>
    public static IReadOnlyList<int> ParseDays(string? raw) =>
        (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => int.TryParse(d, out var day) ? day : 0)
            .Where(d => d is >= 1 and <= 7)
            .Distinct()
            .Order()
            .ToList();

    public static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
            return tz;

        // Contenedores sin tzdata: Colombia no tiene horario de verano (UTC-5 fijo).
        if (string.IsNullOrWhiteSpace(id) || id == DefaultTimeZoneId)
            return TimeZoneInfo.CreateCustomTimeZone(DefaultTimeZoneId, TimeSpan.FromHours(-5), "Colombia", "Colombia");

        return TimeZoneInfo.Local;
    }

    /// <summary>Primera franja estrictamente posterior a <paramref name="afterUtc"/>.</summary>
    public static DateTime? NextSlotAfter(
        IReadOnlyList<TimeOnly> times, IReadOnlyList<int> days, TimeZoneInfo tz, DateTime afterUtc)
    {
        if (times.Count == 0)
            return null;

        afterUtc = AsUtc(afterUtc);
        var startDate = TimeZoneInfo.ConvertTimeFromUtc(afterUtc, tz).Date;
        for (var d = 0; d <= 7; d++)
        {
            var date = startDate.AddDays(d);
            if (!IsAllowedDay(date, days))
                continue;

            foreach (var time in times)
            {
                var slot = ToUtc(date, time, tz);
                if (slot is not null && slot.Value > afterUtc)
                    return slot;
            }
        }

        return null;
    }

    /// <summary>Franja más reciente dentro de (<paramref name="afterUtcExclusive"/>, <paramref name="upToUtcInclusive"/>].</summary>
    public static DateTime? LatestSlotInRange(
        IReadOnlyList<TimeOnly> times,
        IReadOnlyList<int> days,
        TimeZoneInfo tz,
        DateTime afterUtcExclusive,
        DateTime upToUtcInclusive)
    {
        afterUtcExclusive = AsUtc(afterUtcExclusive);
        upToUtcInclusive = AsUtc(upToUtcInclusive);
        if (times.Count == 0 || upToUtcInclusive <= afterUtcExclusive)
            return null;

        var endDate = TimeZoneInfo.ConvertTimeFromUtc(upToUtcInclusive, tz).Date;
        for (var d = 0; d <= 7; d++)
        {
            var date = endDate.AddDays(-d);
            if (!IsAllowedDay(date, days))
                continue;

            for (var i = times.Count - 1; i >= 0; i--)
            {
                var slot = ToUtc(date, times[i], tz);
                if (slot is null || slot.Value > upToUtcInclusive)
                    continue;
                return slot.Value > afterUtcExclusive ? slot : null;
            }
        }

        return null;
    }

    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? AsUtc(DateTime? value) => value is null ? null : AsUtc(value.Value);

    private static bool IsAllowedDay(DateTime localDate, IReadOnlyList<int> days)
    {
        if (days.Count == 0)
            return true;
        var iso = localDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)localDate.DayOfWeek;
        return days.Contains(iso);
    }

    private static DateTime? ToUtc(DateTime localDate, TimeOnly time, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(localDate.Add(time.ToTimeSpan()), DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(local))
            return null; // hora inexistente por cambio de horario
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }
}
