using System.Globalization;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;

namespace dailyTimeApi.Services;

public class ScrapeScheduleService : IScrapeScheduleService
{
    /// <summary>Retraso máximo con el que una franja aún se ejecuta; pasado este margen se omite.</summary>
    public const int LateToleranceMinutes = 10;
    private const int MaxTimes = 24;

    private static readonly HashSet<string> AllowedSlotStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "running", "completed", "failed", "cancelled", "skipped"
    };

    private readonly IScrapeScheduleRepository _repository;

    public ScrapeScheduleService(IScrapeScheduleRepository repository) => _repository = repository;

    public async Task<ScrapeScheduleResponse> GetAsync(CancellationToken cancellationToken = default) =>
        Map(await GetOrCreateAsync(cancellationToken), DateTime.UtcNow);

    public async Task<ScrapeScheduleResponse> UpdateAsync(
        UpdateScrapeScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var times = NormalizeTimes(request.Times);
        var days = NormalizeDays(request.Days);
        if (request.Enabled && times.Count == 0)
            throw new ValidationException("Agrega al menos una hora para activar la ejecución automática.");

        var entity = await GetOrCreateAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var timesValue = string.Join(",", times);
        var daysValue = days.Count == 0 ? null : string.Join(",", days);

        var changed = entity.Enabled != request.Enabled
                      || entity.Times != timesValue
                      || entity.Days != daysValue;
        if (changed)
        {
            entity.Enabled = request.Enabled;
            entity.Times = timesValue;
            entity.Days = daysValue;
            // Las horas que ya pasaron antes del cambio no se ejecutan ni cuentan como omitidas.
            entity.ConfigUpdatedAt = now;
            entity.UpdatedAt = now;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Map(entity, now);
    }

    public async Task<ScrapeScheduleResponse> MarkSlotAsync(
        MarkScrapeSlotRequest request, CancellationToken cancellationToken = default)
    {
        var status = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedSlotStatuses.Contains(status))
            throw new ValidationException("Status inválido. Usa: running, completed, failed, cancelled, skipped.");
        if (request.SlotAt == default)
            throw new ValidationException("SlotAt es obligatorio.");

        var entity = await GetOrCreateAsync(cancellationToken);
        var now = DateTime.UtcNow;
        entity.LastSlotAt = ScrapeSlotCalculator.AsUtc(request.SlotAt);
        entity.LastSlotStatus = status;
        entity.LastSlotMessage = Truncate(request.Message, 1000);
        entity.LastSlotFinishedAt = status switch
        {
            "running" => null,
            // Omitir no consume tiempo: se conserva el fin de la última ejecución real.
            "skipped" => entity.LastSlotFinishedAt,
            _ => ScrapeSlotCalculator.AsUtc(request.FinishedAt) ?? now
        };
        entity.UpdatedAt = now;
        await _repository.SaveChangesAsync(cancellationToken);
        return Map(entity, now);
    }

    private async Task<ScrapeSchedule> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(cancellationToken);
        if (entity is not null)
            return entity;

        var now = DateTime.UtcNow;
        entity = new ScrapeSchedule
        {
            Enabled = false,
            Times = string.Empty,
            TimeZoneId = ScrapeSlotCalculator.DefaultTimeZoneId,
            ConfigUpdatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static ScrapeScheduleResponse Map(ScrapeSchedule entity, DateTime nowUtc)
    {
        var times = ScrapeSlotCalculator.ParseTimes(entity.Times);
        var days = ScrapeSlotCalculator.ParseDays(entity.Days);
        var tz = ScrapeSlotCalculator.ResolveTimeZone(entity.TimeZoneId);

        var configUpdatedAt = ScrapeSlotCalculator.AsUtc(entity.ConfigUpdatedAt);
        var lastSlotAt = ScrapeSlotCalculator.AsUtc(entity.LastSlotAt);
        var lastFinishedAt = ScrapeSlotCalculator.AsUtc(entity.LastSlotFinishedAt);
        var running = entity.LastSlotStatus == "running";

        DateTime? nextSlotAt = null;
        DateTime? missedSlotAt = null;
        if (entity.Enabled && times.Count > 0)
        {
            var floor = nowUtc.AddMinutes(-LateToleranceMinutes);
            // Mientras hay una ejecución en curso, las horas que van llegando se consideran encimadas.
            var busyUntil = running ? nowUtc : lastFinishedAt;
            var handledUntil = Max(lastSlotAt, configUpdatedAt);
            var reference = Max(handledUntil, busyUntil, floor);

            nextSlotAt = ScrapeSlotCalculator.NextSlotAfter(times, days, tz, reference);
            if (!running)
            {
                missedSlotAt = ScrapeSlotCalculator.LatestSlotInRange(
                    times, days, tz, handledUntil, Max(floor, busyUntil));
            }
        }

        return new ScrapeScheduleResponse
        {
            Enabled = entity.Enabled,
            Times = times.Select(t => t.ToString("HH:mm", CultureInfo.InvariantCulture)).ToList(),
            Days = days,
            TimeZoneId = entity.TimeZoneId,
            ConfigUpdatedAt = configUpdatedAt,
            LastSlotAt = lastSlotAt,
            LastSlotStatus = entity.LastSlotStatus,
            LastSlotMessage = entity.LastSlotMessage,
            LastSlotFinishedAt = lastFinishedAt,
            NextSlotAt = nextSlotAt,
            MissedSlotAt = missedSlotAt,
            LateToleranceMinutes = LateToleranceMinutes,
            ServerUtc = nowUtc
        };
    }

    private static List<string> NormalizeTimes(IEnumerable<string>? raw)
    {
        var result = new SortedSet<TimeOnly>();
        foreach (var value in raw ?? [])
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            var trimmed = value.Trim();
            if (!TimeOnly.TryParseExact(trimmed, ["HH:mm", "H:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                throw new ValidationException($"Hora inválida: '{trimmed}'. Usa el formato HH:mm (ej. 07:30).");
            result.Add(time);
        }

        if (result.Count > MaxTimes)
            throw new ValidationException($"Máximo {MaxTimes} horas por día.");

        return result.Select(t => t.ToString("HH:mm", CultureInfo.InvariantCulture)).ToList();
    }

    private static List<int> NormalizeDays(IEnumerable<int>? raw)
    {
        var days = (raw ?? []).Distinct().Order().ToList();
        if (days.Any(d => d is < 1 or > 7))
            throw new ValidationException("Días inválidos. Usa 1 (lunes) a 7 (domingo).");
        // Los 7 días equivalen a "todos".
        return days.Count == 7 ? [] : days;
    }

    private static DateTime Max(params DateTime?[] values) =>
        values.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(DateTime.MinValue).Max();

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
