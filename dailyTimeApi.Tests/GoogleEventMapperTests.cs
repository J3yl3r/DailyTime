using System.Text.Json.Nodes;
using dailyTimeApi.Services.Google;

namespace dailyTimeApi.Tests;

/// <summary>
/// El mapeador es la parte de la integración con Google que no toca red ni base de datos:
/// traducir horarios entre los dos modelos es donde se concentran los casos raros.
/// </summary>
public class GoogleEventMapperTests
{
    private static readonly TimeZoneInfo Bogota = GoogleEventMapper.ResolveTimeZone("America/Bogota");

    private static JsonObject TimedEvent(string start, string end) => new()
    {
        ["id"] = "evt-1",
        ["etag"] = "\"abc\"",
        ["status"] = "confirmed",
        ["summary"] = "Reunión",
        ["updated"] = "2026-09-19T10:00:00.000Z",
        ["start"] = new JsonObject { ["dateTime"] = start },
        ["end"] = new JsonObject { ["dateTime"] = end }
    };

    [Fact]
    public void ReadSchedule_ConvierteHorasAlaZonaConfigurada()
    {
        var json = TimedEvent("2026-09-19T13:00:00Z", "2026-09-19T14:30:00Z");

        var schedule = GoogleEventMapper.ReadSchedule(json, Bogota);

        Assert.NotNull(schedule);
        Assert.Equal(new DateOnly(2026, 9, 19), schedule!.WorkDate);
        Assert.Equal(new TimeOnly(8, 0), schedule.StartTime);
        Assert.Equal(new TimeOnly(9, 30), schedule.EndTime);
        Assert.False(schedule.IsAllDay);
    }

    [Fact]
    public void ReadSchedule_EventoDeDiaCompletoQuedaSinHoras()
    {
        var json = new JsonObject
        {
            ["id"] = "evt-2",
            ["start"] = new JsonObject { ["date"] = "2026-09-19" },
            ["end"] = new JsonObject { ["date"] = "2026-09-20" }
        };

        var schedule = GoogleEventMapper.ReadSchedule(json, Bogota);

        Assert.NotNull(schedule);
        Assert.Equal(new DateOnly(2026, 9, 19), schedule!.WorkDate);
        Assert.True(schedule.IsAllDay);
        Assert.Null(schedule.EndTime);
    }

    [Fact]
    public void ReadSchedule_EventoQueCruzaMedianocheSeRecortaAlDia()
    {
        // 22:00 a 01:00 hora de Bogotá: el modelo no admite un bloque que pase de día.
        var json = TimedEvent("2026-09-19T22:00:00-05:00", "2026-09-20T01:00:00-05:00");

        var schedule = GoogleEventMapper.ReadSchedule(json, Bogota);

        Assert.NotNull(schedule);
        Assert.Equal(new DateOnly(2026, 9, 19), schedule!.WorkDate);
        Assert.Equal(new TimeOnly(22, 0), schedule.StartTime);
        Assert.Equal(new TimeOnly(23, 59), schedule.EndTime);
    }

    [Fact]
    public void ReadSchedule_EventoSinDuracionRecibeUnaDuracionMinima()
    {
        var json = TimedEvent("2026-09-19T09:00:00-05:00", "2026-09-19T09:00:00-05:00");

        var schedule = GoogleEventMapper.ReadSchedule(json, Bogota);

        Assert.NotNull(schedule);
        Assert.Equal(new TimeOnly(9, 0), schedule!.StartTime);
        Assert.Equal(new TimeOnly(9, 30), schedule.EndTime);
    }

    [Fact]
    public void ReadSchedule_EventoEnElUltimoMinutoDelDiaPasaADiaCompleto()
    {
        var json = TimedEvent("2026-09-19T23:59:00-05:00", "2026-09-20T00:30:00-05:00");

        var schedule = GoogleEventMapper.ReadSchedule(json, Bogota);

        Assert.NotNull(schedule);
        Assert.True(schedule!.IsAllDay);
    }

    [Fact]
    public void BuildEventBody_ConHorasMandaLaHoraLocalYLaZona()
    {
        var schedule = new LocalSchedule(new DateOnly(2026, 9, 19), new TimeOnly(8, 0), new TimeOnly(9, 30));

        var body = GoogleEventMapper.BuildEventBody(
            GoogleSyncKinds.Task, 42, "Revisar PR", "detalle", schedule, "America/Bogota");

        Assert.Equal("Revisar PR", body["summary"]!.GetValue<string>());
        Assert.Equal("2026-09-19T08:00:00", body["start"]!["dateTime"]!.GetValue<string>());
        Assert.Equal("America/Bogota", body["start"]!["timeZone"]!.GetValue<string>());
        Assert.Equal("2026-09-19T09:30:00", body["end"]!["dateTime"]!.GetValue<string>());
        // El campo del otro modo viaja en null para que un PATCH pueda cambiar de tipo.
        Assert.Null(body["start"]!["date"]);
        Assert.Equal("42", body["extendedProperties"]!["private"]![GoogleEventMapper.IdProperty]!.GetValue<string>());
        Assert.Equal(
            GoogleSyncKinds.Task,
            body["extendedProperties"]!["private"]![GoogleEventMapper.KindProperty]!.GetValue<string>());
    }

    [Fact]
    public void BuildEventBody_SinHorasUsaFechaFinalExclusiva()
    {
        var schedule = new LocalSchedule(new DateOnly(2026, 9, 19), null, null);

        var body = GoogleEventMapper.BuildEventBody(
            GoogleSyncKinds.Task, 7, "Entregar informe", null, schedule, "America/Bogota");

        Assert.Equal("2026-09-19", body["start"]!["date"]!.GetValue<string>());
        Assert.Equal("2026-09-20", body["end"]!["date"]!.GetValue<string>());
        Assert.Null(body["start"]!["dateTime"]);
        Assert.Null(body["description"]);
    }

    [Fact]
    public void ReadEvent_MarcaLosCanceladosYNoLesCalculaHorario()
    {
        var json = TimedEvent("2026-09-19T13:00:00Z", "2026-09-19T14:00:00Z");
        json["status"] = "cancelled";

        var snapshot = GoogleEventMapper.ReadEvent(json, Bogota);

        Assert.NotNull(snapshot);
        Assert.True(snapshot!.IsCancelled);
        Assert.Null(snapshot.Schedule);
    }

    [Fact]
    public void ReadEvent_LeeLaMarcaDeOrigenDeDailyTime()
    {
        var json = TimedEvent("2026-09-19T13:00:00Z", "2026-09-19T14:00:00Z");
        json["extendedProperties"] = new JsonObject
        {
            ["private"] = new JsonObject
            {
                [GoogleEventMapper.KindProperty] = GoogleSyncKinds.Note,
                [GoogleEventMapper.IdProperty] = "15"
            }
        };

        var snapshot = GoogleEventMapper.ReadEvent(json, Bogota);

        Assert.Equal(GoogleSyncKinds.Note, snapshot!.DailyTimeKind);
        Assert.Equal(15, snapshot.DailyTimeId);
        Assert.Equal(new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc), snapshot.UpdatedAt);
    }

    [Fact]
    public void ReadEvent_SinIdNoEsUtilizable()
    {
        var json = new JsonObject { ["summary"] = "sin id" };

        Assert.Null(GoogleEventMapper.ReadEvent(json, Bogota));
    }

    [Fact]
    public void ResolveTimeZone_ZonaDesconocidaCaeEnUtc()
    {
        Assert.Equal(TimeZoneInfo.Utc, GoogleEventMapper.ResolveTimeZone("Marte/Olimpo"));
        Assert.Equal(TimeZoneInfo.Utc, GoogleEventMapper.ResolveTimeZone(null));
    }
}
