using System.Text.Json;
using dailyTimeApi.Models.Triage;

namespace dailyTimeApi.Services.Ai;

public class OfferAiException : Exception
{
    public OfferAiException(string message, bool isFatal = false, Exception? inner = null)
        : base(message, inner)
    {
        IsFatal = isFatal;
    }

    /// <summary>Error que afecta a todas las ofertas (clave inválida, modelo inexistente, sin red): detiene la pasada.</summary>
    public bool IsFatal { get; }
}

public sealed class OfferAiNotConfiguredException : OfferAiException
{
    public OfferAiNotConfiguredException()
        : base("Falta la clave de Gemini: configúrala en user secrets como Gemini:ApiKey.", isFatal: true)
    {
    }
}

public sealed class OfferAiQuotaException : OfferAiException
{
    public OfferAiQuotaException(string message, TimeSpan? retryAfter, bool isDaily)
        : base(message, isFatal: true)
    {
        RetryAfter = retryAfter;
        IsDaily = isDaily;
    }

    public TimeSpan? RetryAfter { get; }

    /// <summary>true = cuota diaria agotada (se renueva a medianoche del Pacífico).</summary>
    public bool IsDaily { get; }
}

/// <summary>Lectura, escritura y saneo del JSON de <see cref="OfferAiAnalysis"/>.</summary>
public static class OfferAiJson
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] Verdicts = ["apply", "maybe", "skip"];
    private static readonly string[] MetValues = ["yes", "partial", "no", "unknown"];
    private static readonly string[] Seniorities = ["junior", "semi-senior", "senior", "lead", "unknown"];
    private static readonly string[] EnglishLevels = ["none", "basic", "intermediate", "advanced", "unknown"];
    private static readonly string[] Modalities = ["remote", "hybrid", "onsite", "unknown"];
    private static readonly string[] Periods = ["month", "year", "hour", "unknown"];

    public static string Serialize(OfferAiAnalysis analysis) => JsonSerializer.Serialize(analysis);

    public static OfferAiAnalysis? TryRead(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<OfferAiAnalysis>(json, ReadOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static OfferAiAnalysis Deserialize(string json) =>
        JsonSerializer.Deserialize<OfferAiAnalysis>(json, ReadOptions)
        ?? throw new JsonException("Análisis vacío.");

    /// <summary>Recorta textos y listas y fuerza valores permitidos: la respuesta del modelo no es de fiar.</summary>
    public static OfferAiAnalysis Sanitize(OfferAiAnalysis analysis) => new()
    {
        Verdict = OneOf(analysis.Verdict, Verdicts, "maybe"),
        Summary = Truncate(analysis.Summary, 500),
        MandatoryRequirements = (analysis.MandatoryRequirements ?? new List<OfferAiRequirement>())
            .Where(r => r is not null && !string.IsNullOrWhiteSpace(r.Requirement))
            .Take(8)
            .Select(r => new OfferAiRequirement
            {
                Requirement = Truncate(r.Requirement, 200),
                Met = OneOf(r.Met, MetValues, "unknown")
            })
            .ToList(),
        MissingMustHaves = CleanList(analysis.MissingMustHaves, 5),
        Seniority = OneOf(analysis.Seniority, Seniorities, "unknown"),
        RequiredYears = Math.Clamp(analysis.RequiredYears, 0, 30),
        EnglishLevel = OneOf(analysis.EnglishLevel, EnglishLevels, "unknown"),
        WorkModality = OneOf(analysis.WorkModality, Modalities, "unknown"),
        LocationRestriction = Truncate(analysis.LocationRestriction, 200),
        Salary = new OfferAiSalary
        {
            Min = Math.Max(0, analysis.Salary?.Min ?? 0),
            Max = Math.Max(0, analysis.Salary?.Max ?? 0),
            Currency = Truncate(analysis.Salary?.Currency, 10).ToUpperInvariant(),
            Period = OneOf(analysis.Salary?.Period, Periods, "unknown")
        },
        RedFlags = CleanList(analysis.RedFlags, 5)
    };

    private static string OneOf(string? value, string[] allowed, string fallback)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return allowed.Contains(normalized) ? normalized : fallback;
    }

    private static List<string> CleanList(IEnumerable<string>? values, int max) =>
        (values ?? Enumerable.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Truncate(v, 200))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();

    internal static string Truncate(string? value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

/// <summary>La cuota diaria gratuita de Gemini se renueva a medianoche del Pacífico.</summary>
public static class OfferAiClock
{
    private static readonly TimeZoneInfo Pacific = ResolvePacific();

    public static DateTime DayStartUtc(DateTime nowUtc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), Pacific);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local.Date, DateTimeKind.Unspecified), Pacific);
    }

    public static DateTime NextResetUtc(DateTime nowUtc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), Pacific);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local.Date.AddDays(1), DateTimeKind.Unspecified), Pacific);
    }

    private static TimeZoneInfo ResolvePacific()
    {
        foreach (var id in new[] { "America/Los_Angeles", "Pacific Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
                return tz;
        }
        return TimeZoneInfo.CreateCustomTimeZone("Pacific", TimeSpan.FromHours(-8), "Pacific", "Pacific");
    }
}

/// <summary>
/// Estado compartido del análisis con IA: pausa por cuota, último error y la señal que despierta al
/// servicio en segundo plano (sin sondeo).
/// </summary>
public sealed class OfferAiState
{
    private readonly object _gate = new();
    private TaskCompletionSource _runRequested = NewSignal();
    private Timer? _wakeTimer;

    public DateTime? PausedUntilUtc { get; private set; }
    public string? PauseReason { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? LastRunAt { get; private set; }
    public bool IsRunning { get; private set; }

    /// <summary>Hay una pasada pedida que el servicio en segundo plano aún no empezó.</summary>
    public bool IsRunRequested
    {
        get
        {
            lock (_gate)
                return _runRequested.Task.IsCompleted;
        }
    }

    public void RequestRun()
    {
        lock (_gate)
            _runRequested.TrySetResult();
    }

    public Task WaitForRunRequestAsync(CancellationToken cancellationToken)
    {
        Task task;
        lock (_gate)
            task = _runRequested.Task;
        return task.WaitAsync(cancellationToken);
    }

    /// <summary>Consume la solicitud actual; las que lleguen durante el proceso provocan otra pasada.</summary>
    public void BeginRun()
    {
        lock (_gate)
        {
            if (_runRequested.Task.IsCompleted)
                _runRequested = NewSignal();
            IsRunning = true;
        }
    }

    public void EndRun(DateTime nowUtc)
    {
        lock (_gate)
        {
            IsRunning = false;
            LastRunAt = nowUtc;
        }
    }

    /// <summary>Pausa hasta <paramref name="untilUtc"/> y programa una nueva pasada para ese momento.</summary>
    public void Pause(DateTime untilUtc, string reason)
    {
        lock (_gate)
        {
            PausedUntilUtc = untilUtc;
            PauseReason = reason;
            _wakeTimer?.Dispose();
            var due = untilUtc - DateTime.UtcNow;
            _wakeTimer = new Timer(_ => RequestRun(), null, due < TimeSpan.Zero ? TimeSpan.Zero : due, Timeout.InfiniteTimeSpan);
        }
    }

    public void ClearPauseIfExpired(DateTime nowUtc)
    {
        lock (_gate)
        {
            if (PausedUntilUtc is { } until && until <= nowUtc)
            {
                PausedUntilUtc = null;
                PauseReason = null;
            }
        }
    }

    public void SetError(string? error)
    {
        lock (_gate)
            LastError = error;
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
