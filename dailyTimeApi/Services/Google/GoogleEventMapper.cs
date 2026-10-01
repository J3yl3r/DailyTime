using System.Globalization;
using System.Text.Json.Nodes;

namespace dailyTimeApi.Services.Google;

/// <summary>Tipo de elemento de DailyTime que respalda un evento.</summary>
public static class GoogleSyncKinds
{
    public const string Task = "task";
    public const string Note = "note";
}

/// <summary>Origen de un elemento sincronizado.</summary>
public static class GoogleSyncSources
{
    public const string Local = "local";
    public const string Google = "google";
}

/// <summary>Horario de un elemento tal como lo entiende DailyTime.</summary>
public record LocalSchedule(DateOnly WorkDate, TimeOnly? StartTime, TimeOnly? EndTime)
{
    public bool IsAllDay => StartTime is null;
}

/// <summary>Lectura ya normalizada de un evento devuelto por Google.</summary>
public record GoogleEventSnapshot(
    string Id,
    string? Etag,
    bool IsCancelled,
    string Summary,
    string? Description,
    DateTime UpdatedAt,
    LocalSchedule? Schedule,
    string? DailyTimeKind,
    int? DailyTimeId,
    // Color elegido en Google para este evento; null si usa el del calendario.
    string? ColorId,
    string? HtmlLink);

/// <summary>
/// Traducción entre <c>TaskItem</c>/<c>Note</c> y los eventos de Google Calendar. Es una función
/// pura (sin red ni base de datos) para poder probarla, igual que <c>OfferScorer</c>.
/// </summary>
public static class GoogleEventMapper
{
    public const string KindProperty = "dailyTimeKind";
    public const string IdProperty = "dailyTimeId";

    /// <summary>Duración que se da a un evento de Google sin duración utilizable.</summary>
    private const int FallbackMinutes = 30;

    private static readonly TimeOnly LastMinuteOfDay = new(23, 59);

    /// <summary>Cuerpo JSON del evento que representa un elemento de DailyTime.</summary>
    public static JsonObject BuildEventBody(
        string kind, int localId, string title, string? description, LocalSchedule schedule, string timeZoneId)
    {
        var body = new JsonObject
        {
            ["summary"] = string.IsNullOrWhiteSpace(title) ? "(sin título)" : title.Trim(),
            ["description"] = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ["start"] = BuildEndpoint(schedule.WorkDate, schedule.StartTime, timeZoneId),
            ["extendedProperties"] = new JsonObject
            {
                ["private"] = new JsonObject
                {
                    [KindProperty] = kind,
                    [IdProperty] = localId.ToString(CultureInfo.InvariantCulture)
                }
            }
        };

        body["end"] = schedule.IsAllDay
            // En un evento de día completo la fecha final es exclusiva.
            ? BuildEndpoint(schedule.WorkDate.AddDays(1), null, timeZoneId)
            : BuildEndpoint(schedule.WorkDate, schedule.EndTime, timeZoneId);

        return body;
    }

    /// <summary>
    /// Extremo de un evento. Se mandan explícitamente en null los campos del otro modo para que
    /// un PATCH pueda convertir un evento con hora en uno de día completo y al revés.
    /// </summary>
    private static JsonObject BuildEndpoint(DateOnly date, TimeOnly? time, string timeZoneId) =>
        time is null
            ? new JsonObject
            {
                ["date"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["dateTime"] = null,
                ["timeZone"] = null
            }
            : new JsonObject
            {
                // Hora local sin desfase: el desfase lo resuelve Google con timeZone.
                ["dateTime"] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T" +
                               time.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                ["timeZone"] = timeZoneId,
                ["date"] = null
            };

    /// <summary>Lee un evento de la respuesta de Google. Devuelve null si no es utilizable.</summary>
    public static GoogleEventSnapshot? ReadEvent(JsonObject json, TimeZoneInfo timeZone)
    {
        var id = json["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var isCancelled = string.Equals(
            json["status"]?.GetValue<string>(), "cancelled", StringComparison.OrdinalIgnoreCase);

        var privateProps = json["extendedProperties"]?["private"] as JsonObject;
        var kind = privateProps?[KindProperty]?.GetValue<string>();
        int? localId = null;
        if (privateProps?[IdProperty]?.GetValue<string>() is { } rawId &&
            int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
        {
            localId = parsedId;
        }

        return new GoogleEventSnapshot(
            id!,
            json["etag"]?.GetValue<string>(),
            isCancelled,
            json["summary"]?.GetValue<string>() ?? "(sin título)",
            json["description"]?.GetValue<string>(),
            ReadUpdated(json),
            isCancelled ? null : ReadSchedule(json, timeZone),
            kind,
            localId,
            json["colorId"]?.GetValue<string>(),
            json["htmlLink"]?.GetValue<string>());
    }

    private static DateTime ReadUpdated(JsonObject json)
    {
        var raw = json["updated"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(raw) &&
            DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.UtcDateTime;
        }
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Convierte los extremos del evento al horario de DailyTime, respetando las restricciones
    /// del modelo: o las dos horas o ninguna, y la final posterior a la inicial dentro del día.
    /// </summary>
    public static LocalSchedule? ReadSchedule(JsonObject json, TimeZoneInfo timeZone)
    {
        var start = json["start"] as JsonObject;
        var end = json["end"] as JsonObject;
        if (start is null)
            return null;

        var startDateOnly = start["date"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(startDateOnly))
        {
            // Día completo. Uno de varios días se ancla en su primer día: el modelo no
            // representa un elemento que abarque un rango.
            return DateOnly.TryParseExact(startDateOnly, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var allDay)
                ? new LocalSchedule(allDay, null, null)
                : null;
        }

        if (!TryReadLocal(start, timeZone, out var startLocal))
            return null;

        var workDate = DateOnly.FromDateTime(startLocal);
        var startTime = TimeOnly.FromDateTime(startLocal);

        TimeOnly endTime;
        if (end is not null && TryReadLocal(end, timeZone, out var endLocal) &&
            DateOnly.FromDateTime(endLocal) == workDate)
        {
            endTime = TimeOnly.FromDateTime(endLocal);
        }
        else
        {
            // Termina otro día (o no trae fin): se recorta al final del mismo día.
            endTime = LastMinuteOfDay;
        }

        if (endTime <= startTime)
        {
            var candidate = startTime.ToTimeSpan() + TimeSpan.FromMinutes(FallbackMinutes);
            endTime = candidate >= TimeSpan.FromHours(24) ? LastMinuteOfDay : TimeOnly.FromTimeSpan(candidate);
        }

        // Un evento que empieza en el último minuto del día no cabe como bloque con horas.
        return endTime <= startTime
            ? new LocalSchedule(workDate, null, null)
            : new LocalSchedule(workDate, startTime, endTime);
    }

    private static bool TryReadLocal(JsonObject endpoint, TimeZoneInfo timeZone, out DateTime local)
    {
        local = default;
        var raw = endpoint["dateTime"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return false;

        local = TimeZoneInfo.ConvertTime(parsed, timeZone).DateTime;
        return true;
    }

    /// <summary>Zona horaria configurada, con vuelta atrás a UTC si el identificador no existe.</summary>
    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>true si el contenido del evento ya coincide con el horario y textos dados.</summary>
    public static bool Matches(GoogleEventSnapshot snapshot, string title, string? description, LocalSchedule schedule) =>
        string.Equals(snapshot.Summary?.Trim(), title?.Trim(), StringComparison.Ordinal) &&
        string.Equals(
            string.IsNullOrWhiteSpace(snapshot.Description) ? null : snapshot.Description.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            StringComparison.Ordinal) &&
        snapshot.Schedule == schedule;
}
