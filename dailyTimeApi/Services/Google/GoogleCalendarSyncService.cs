using System.Text.Json.Nodes;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services;
using Microsoft.Extensions.Options;

namespace dailyTimeApi.Services.Google;

public record GoogleSyncResult(int Pushed, int Pulled, int Deleted, string? Message)
{
    public static GoogleSyncResult Idle(string message) => new(0, 0, 0, message);
}

public interface IGoogleCalendarSyncService
{
    Task<GoogleSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Sincronización bidireccional con Google Calendar dentro de una ventana de días.
/// <para>
/// Cada pasada hace tres cosas, en este orden: propaga los borrados locales, trae los cambios
/// remotos y empuja los locales. El orden importa — borrar primero evita que un evento cuyo
/// elemento ya no existe vuelva a crearse, y traer antes de empujar deja que el conflicto se
/// resuelva una sola vez.
/// </para>
/// <para>
/// Conflictos: si los dos lados cambiaron desde la última sincronización gana el más reciente
/// (<c>UpdatedAt</c> local contra <c>updated</c> de Google).
/// </para>
/// </summary>
public class GoogleCalendarSyncService : IGoogleCalendarSyncService
{
    /// <summary>Margen del corte incremental, para absorber desfases de reloj con Google.</summary>
    private static readonly TimeSpan CutoffSafety = TimeSpan.FromMinutes(2);

    /// <summary>Se renueva el token si le quedan menos de esto.</summary>
    private static readonly TimeSpan TokenSkew = TimeSpan.FromMinutes(2);

    private readonly IGoogleCalendarRepository _repository;
    private readonly IWorkItemStatusRepository _statusRepository;
    private readonly IWorkItemCategoryRepository _categoryRepository;
    private readonly IGoogleOAuthClient _oauth;
    private readonly IGoogleCalendarClient _calendar;
    private readonly GoogleCalendarOptions _options;
    private readonly ILogger<GoogleCalendarSyncService> _logger;

    public GoogleCalendarSyncService(
        IGoogleCalendarRepository repository,
        IWorkItemStatusRepository statusRepository,
        IWorkItemCategoryRepository categoryRepository,
        IGoogleOAuthClient oauth,
        IGoogleCalendarClient calendar,
        IOptions<GoogleCalendarOptions> options,
        ILogger<GoogleCalendarSyncService> logger)
    {
        _repository = repository;
        _statusRepository = statusRepository;
        _categoryRepository = categoryRepository;
        _oauth = oauth;
        _calendar = calendar;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountAsync(cancellationToken);
        if (account is null)
            return GoogleSyncResult.Idle("No hay ninguna cuenta de Google conectada.");
        if (!account.SyncEnabled)
            return GoogleSyncResult.Idle("La sincronización está pausada.");

        var startedAt = DateTime.UtcNow;
        account.LastSyncStatus = "running";
        await _repository.SaveChangesAsync(cancellationToken);

        try
        {
            var token = await EnsureAccessTokenAsync(account, cancellationToken);
            var timeZone = GoogleEventMapper.ResolveTimeZone(account.TimeZoneId);
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
            var from = today.AddDays(-account.PastDays);
            var to = today.AddDays(account.FutureDays);

            var deleted = await PushDeletionsAsync(account, token, cancellationToken);
            var colores = new ColorResolver(_calendar, token, account.CalendarId, _logger);
            var pulled = await PullAsync(account, token, timeZone, from, to, colores, cancellationToken);
            var pushed = await PushAsync(account, token, timeZone, from, to, cancellationToken);

            // El corte se ancla al inicio de la pasada: lo que cambie mientras corre se recoge
            // en la siguiente, en lugar de perderse.
            account.PullCutoffAt = startedAt - CutoffSafety;
            account.LastSyncAt = DateTime.UtcNow;
            account.LastSyncStatus = "ok";
            account.LastSyncMessage = null;
            account.LastPushedCount = pushed;
            account.LastPulledCount = pulled;
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);

            return new GoogleSyncResult(pushed, pulled, deleted, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var message = ex is GoogleReauthRequiredException or GoogleApiException
                ? ex.Message
                : "Error sincronizando con Google: " + ex.Message;
            _logger.LogError(ex, "Fallo la sincronización con Google Calendar.");

            account.LastSyncAt = DateTime.UtcNow;
            account.LastSyncStatus = "error";
            account.LastSyncMessage = Truncate(message, 1000);
            account.UpdatedAt = DateTime.UtcNow;
            // Si hizo falta reautorizar, la cuenta queda en pausa hasta que el usuario reconecte.
            if (ex is GoogleReauthRequiredException)
                account.SyncEnabled = false;
            await _repository.SaveChangesAsync(CancellationToken.None);

            return new GoogleSyncResult(0, 0, 0, message);
        }
    }

    // --- Token ------------------------------------------------------------------------

    private async Task<string> EnsureAccessTokenAsync(
        GoogleCalendarAccount account, CancellationToken cancellationToken)
    {
        if (account.AccessTokenExpiresAt - TokenSkew > DateTime.UtcNow &&
            !string.IsNullOrWhiteSpace(account.AccessToken))
        {
            return account.AccessToken;
        }

        var tokens = await _oauth.RefreshAsync(account.RefreshToken, cancellationToken);
        account.AccessToken = tokens.AccessToken;
        account.AccessTokenExpiresAt = tokens.ExpiresAt;
        account.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        return tokens.AccessToken;
    }

    // --- Borrados locales -> Google ----------------------------------------------------

    private async Task<int> PushDeletionsAsync(
        GoogleCalendarAccount account, string token, CancellationToken cancellationToken)
    {
        var pending = await _repository.GetPendingDeletionsAsync(cancellationToken);
        if (pending.Count == 0)
            return 0;

        var done = 0;
        foreach (var deletion in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _calendar.DeleteEventAsync(
                    token, deletion.CalendarId, deletion.GoogleEventId, cancellationToken);
                _repository.RemoveDeletion(deletion);
                done++;
            }
            catch (GoogleApiException ex)
            {
                deletion.Attempts++;
                deletion.LastError = Truncate(ex.Message, 500);
                // Tras varios intentos se abandona: un evento que Google no deja borrar no
                // puede bloquear el resto de la sincronización para siempre.
                if (deletion.Attempts >= 5)
                {
                    _logger.LogWarning(
                        "Se abandona el borrado del evento {EventId} en Google: {Message}",
                        deletion.GoogleEventId, ex.Message);
                    _repository.RemoveDeletion(deletion);
                }
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return done;
    }

    // --- Google -> DailyTime ------------------------------------------------------------

    private async Task<int> PullAsync(
        GoogleCalendarAccount account,
        string token,
        TimeZoneInfo timeZone,
        DateOnly from,
        DateOnly to,
        ColorResolver colores,
        CancellationToken cancellationToken)
    {
        var timeMin = ToUtc(from, TimeOnly.MinValue, timeZone);
        var timeMax = ToUtc(to.AddDays(1), TimeOnly.MinValue, timeZone);

        var applied = 0;
        string? pageToken = null;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await _calendar.ListEventsAsync(
                token, account.CalendarId, timeMin, timeMax, account.PullCutoffAt, pageToken, cancellationToken);

            foreach (var node in (page["items"] as JsonArray) ?? new JsonArray())
            {
                if (node is not JsonObject eventJson)
                    continue;
                var snapshot = GoogleEventMapper.ReadEvent(eventJson, timeZone);
                if (snapshot is null)
                    continue;
                if (await ApplyRemoteEventAsync(account, token, snapshot, colores, cancellationToken))
                    applied++;
            }

            pageToken = page["nextPageToken"]?.GetValue<string>();
            await _repository.SaveChangesAsync(cancellationToken);
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return applied;
    }

    private async Task<bool> ApplyRemoteEventAsync(
        GoogleCalendarAccount account,
        string token,
        GoogleEventSnapshot snapshot,
        ColorResolver colores,
        CancellationToken cancellationToken)
    {
        var task = await _repository.GetTaskByEventIdAsync(snapshot.Id, cancellationToken);
        var note = task is null ? await _repository.GetNoteByEventIdAsync(snapshot.Id, cancellationToken) : null;

        if (snapshot.IsCancelled)
        {
            if (task is not null)
                return await RemoveLocalTaskAsync(task, cancellationToken);
            if (note is not null)
                return await RemoveLocalNoteAsync(note, cancellationToken);
            return false;
        }

        if (snapshot.Schedule is null)
            return false;

        // Se resuelve una vez y se reparte: el color es cosmético, no debe costar una llamada
        // por cada elemento tocado.
        var color = await colores.ResolveAsync(snapshot.ColorId, cancellationToken);

        if (task is not null)
            return ApplyToTask(task, snapshot, color);
        if (note is not null)
            return ApplyToNote(note, snapshot, color);

        // El evento no está ligado a nada conocido.
        if (snapshot.DailyTimeKind is not null && snapshot.DailyTimeId is int localId)
            return await ReconcileOrphanAsync(account, token, snapshot, localId, cancellationToken);

        // Nació en Google: entra como tarea.
        return await CreateTaskFromEventAsync(snapshot, color, cancellationToken);
    }

    /// <summary>
    /// Evento que creamos nosotros pero cuyo elemento local no aparece: o se perdió el enlace
    /// (se vuelve a atar) o el elemento se borró sin dejar lápida (se borra el evento).
    /// </summary>
    private async Task<bool> ReconcileOrphanAsync(
        GoogleCalendarAccount account,
        string token,
        GoogleEventSnapshot snapshot,
        int localId,
        CancellationToken cancellationToken)
    {
        if (snapshot.DailyTimeKind == GoogleSyncKinds.Task)
        {
            var candidate = await _repository.GetTaskByIdAsync(localId, cancellationToken);
            if (candidate is not null && candidate.GoogleEventId is null)
            {
                Relink(candidate, snapshot);
                return true;
            }
        }
        else if (snapshot.DailyTimeKind == GoogleSyncKinds.Note)
        {
            var candidate = await _repository.GetNoteByIdAsync(localId, cancellationToken);
            if (candidate is not null && candidate.GoogleEventId is null)
            {
                Relink(candidate, snapshot);
                return true;
            }
        }

        await _calendar.DeleteEventAsync(token, account.CalendarId, snapshot.Id, cancellationToken);
        return false;
    }

    private static void Relink(IGoogleSyncedItem item, GoogleEventSnapshot snapshot)
    {
        item.GoogleEventId = snapshot.Id;
        item.GoogleEtag = snapshot.Etag;
        item.GoogleUpdatedAt = snapshot.UpdatedAt;
        item.GoogleSyncedAt = DateTime.UtcNow;
    }

    private bool ApplyToTask(TaskItem task, GoogleEventSnapshot snapshot, string? color)
    {
        // El color no es contenido en disputa: lo decide Google y se refresca siempre, aunque
        // el resto del evento no haya cambiado o aunque la edición local sea la que gana.
        var cambioElColor = task.GoogleColor != color;
        task.GoogleColor = color;

        if (!ShouldApplyRemote(task, snapshot))
            return cambioElColor;

        var now = DateTime.UtcNow;
        task.Title = snapshot.Summary;
        task.Content = string.IsNullOrWhiteSpace(snapshot.Description) ? null : snapshot.Description;
        task.WorkDate = snapshot.Schedule!.WorkDate;
        task.StartTime = snapshot.Schedule.StartTime;
        task.EndTime = snapshot.Schedule.EndTime;
        task.UpdatedAt = now;
        task.GoogleEtag = snapshot.Etag;
        task.GoogleUpdatedAt = snapshot.UpdatedAt;
        task.GoogleSyncedAt = now;
        return true;
    }

    private bool ApplyToNote(Note note, GoogleEventSnapshot snapshot, string? color)
    {
        var cambioElColor = note.GoogleColor != color;
        note.GoogleColor = color;

        if (!ShouldApplyRemote(note, snapshot))
            return cambioElColor;

        var now = DateTime.UtcNow;
        note.Title = snapshot.Summary;
        if (!string.IsNullOrWhiteSpace(snapshot.Description))
            note.Content = snapshot.Description;
        note.WorkDate = snapshot.Schedule!.WorkDate;
        note.StartTime = snapshot.Schedule.StartTime;
        note.EndTime = snapshot.Schedule.EndTime;
        note.UpdatedAt = now;
        note.GoogleEtag = snapshot.Etag;
        note.GoogleUpdatedAt = snapshot.UpdatedAt;
        note.GoogleSyncedAt = now;
        return true;
    }

    /// <summary>Resuelve el conflicto: sin cambios remotos no se toca; con los dos lados cambiados gana el más reciente.</summary>
    private bool ShouldApplyRemote(IGoogleSyncedItem item, GoogleEventSnapshot snapshot)
    {
        var remoteChanged = snapshot.UpdatedAt > (item.GoogleUpdatedAt ?? DateTime.MinValue);
        if (!remoteChanged)
            return false;

        var localChanged = item.UpdatedAt > (item.GoogleSyncedAt ?? DateTime.MinValue);
        if (!localChanged)
            return true;

        if (snapshot.UpdatedAt >= item.UpdatedAt)
            return true;

        // Ganó la edición local: no se toca y el empuje de esta misma pasada la manda a Google.
        _logger.LogInformation(
            "Conflicto en el evento {EventId}: se conserva la edición local, más reciente que la de Google.",
            snapshot.Id);
        return false;
    }

    private async Task<bool> CreateTaskFromEventAsync(
        GoogleEventSnapshot snapshot, string? color, CancellationToken cancellationToken)
    {
        var status = await _statusRepository.GetDefaultByItemTypeAsync(
            WorkItemStatusService.ItemTypeTask, cancellationToken);
        var category = await _categoryRepository.GetDefaultByItemTypeAsync(
            WorkItemStatusService.ItemTypeTask, cancellationToken);
        if (status is null || category is null)
        {
            _logger.LogWarning(
                "No hay estado o categoría por defecto de tareas: el evento {EventId} no se pudo traer.",
                snapshot.Id);
            return false;
        }

        var now = DateTime.UtcNow;
        await _repository.AddTaskAsync(new TaskItem
        {
            Title = snapshot.Summary,
            Content = string.IsNullOrWhiteSpace(snapshot.Description) ? null : snapshot.Description,
            WorkDate = snapshot.Schedule!.WorkDate,
            StartTime = snapshot.Schedule.StartTime,
            EndTime = snapshot.Schedule.EndTime,
            StatusId = status.Id,
            CategoryId = category.Id,
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            GoogleEventId = snapshot.Id,
            GoogleEtag = snapshot.Etag,
            GoogleUpdatedAt = snapshot.UpdatedAt,
            GoogleSyncedAt = now,
            SyncSource = GoogleSyncSources.Google,
            GoogleColor = color
        }, cancellationToken);
        return true;
    }

    private async Task<bool> RemoveLocalTaskAsync(TaskItem task, CancellationToken cancellationToken)
    {
        if (await _repository.TaskHasChildrenAsync(task.Id, cancellationToken))
        {
            // No se puede borrar una tarea con subtareas: se desliga del evento y se queda.
            _logger.LogInformation(
                "El evento de la tarea {TaskId} se borró en Google, pero tiene subtareas: se desliga.", task.Id);
            ClearMarks(task);
            return true;
        }

        _repository.RemoveTask(task);
        return true;
    }

    private async Task<bool> RemoveLocalNoteAsync(Note note, CancellationToken cancellationToken)
    {
        if (await _repository.NoteHasChildrenAsync(note.Id, cancellationToken))
        {
            _logger.LogInformation(
                "El evento de la nota {NoteId} se borró en Google, pero tiene subnotas: se desliga.", note.Id);
            ClearMarks(note);
            return true;
        }

        _repository.RemoveNote(note);
        return true;
    }

    private static void ClearMarks(IGoogleSyncedItem item)
    {
        item.GoogleEventId = null;
        item.GoogleEtag = null;
        item.GoogleUpdatedAt = null;
        item.GoogleSyncedAt = null;
        item.GoogleColor = null;
    }

    // --- DailyTime -> Google ------------------------------------------------------------

    private async Task<int> PushAsync(
        GoogleCalendarAccount account,
        string token,
        TimeZoneInfo timeZone,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var pushed = 0;

        foreach (var task in await _repository.GetTasksInWindowAsync(from, to, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var schedule = new LocalSchedule(task.WorkDate, task.StartTime, task.EndTime);
            if (!IsTaskSyncable(account, schedule) || !NeedsPush(task))
                continue;
            if (await PushItemAsync(
                    account, token, task, GoogleSyncKinds.Task, task.Title, task.Content, schedule, cancellationToken))
            {
                pushed++;
            }
        }

        foreach (var note in await _repository.GetNotesInWindowAsync(from, to, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!account.SyncTimedNotes || note.WorkDate is null || note.StartTime is null)
                continue;
            if (!NeedsPush(note))
                continue;
            var schedule = new LocalSchedule(note.WorkDate.Value, note.StartTime, note.EndTime);
            if (await PushItemAsync(
                    account, token, note, GoogleSyncKinds.Note, NoteTitle(note), note.Content, schedule,
                    cancellationToken))
            {
                pushed++;
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return pushed;
    }

    private static bool IsTaskSyncable(GoogleCalendarAccount account, LocalSchedule schedule) =>
        schedule.IsAllDay ? account.SyncAllDayTasks : account.SyncTimedTasks;

    private static bool NeedsPush(IGoogleSyncedItem item) =>
        item.GoogleEventId is null || item.UpdatedAt > (item.GoogleSyncedAt ?? DateTime.MinValue);

    private static string NoteTitle(Note note)
    {
        if (!string.IsNullOrWhiteSpace(note.Title))
            return note.Title!.Trim();
        var firstLine = note.Content.Split('\n').FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(firstLine))
            return "(nota sin título)";
        return firstLine.Length <= 120 ? firstLine : firstLine[..120];
    }

    private async Task<bool> PushItemAsync(
        GoogleCalendarAccount account,
        string token,
        IGoogleSyncedItem item,
        string kind,
        string title,
        string? description,
        LocalSchedule schedule,
        CancellationToken cancellationToken)
    {
        var body = GoogleEventMapper.BuildEventBody(
            kind, item.Id, title, description, schedule, account.TimeZoneId);

        try
        {
            var response = item.GoogleEventId is null
                ? await _calendar.InsertEventAsync(token, account.CalendarId, body, cancellationToken)
                : await _calendar.PatchEventAsync(
                    token, account.CalendarId, item.GoogleEventId, body, cancellationToken);

            item.GoogleEventId = response["id"]?.GetValue<string>() ?? item.GoogleEventId;
            item.GoogleEtag = response["etag"]?.GetValue<string>();
            item.GoogleUpdatedAt = GoogleEventMapper.ReadEvent(response, TimeZoneInfo.Utc)?.UpdatedAt;
            item.GoogleSyncedAt = DateTime.UtcNow;
            item.SyncSource ??= GoogleSyncSources.Local;
            return true;
        }
        catch (GoogleApiException ex) when (ex.StatusCode is 404 or 410)
        {
            // El evento ya no existe en Google: se vuelve a crear en la próxima pasada.
            _logger.LogInformation(
                "El evento {EventId} ya no existe en Google; se recreará.", item.GoogleEventId);
            item.GoogleEventId = null;
            item.GoogleEtag = null;
            item.GoogleUpdatedAt = null;
            item.GoogleSyncedAt = null;
            return false;
        }
    }

    private static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        // En las zonas con horario de verano hay días en los que la medianoche no existe.
        if (timeZone.IsInvalidTime(local))
            local = local.AddHours(1);
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    /// <summary>
    /// Traduce el <c>colorId</c> de un evento al color en hexadecimal. Carga la paleta y el
    /// color del calendario una sola vez por pasada, y solo si algún evento lo necesita: la
    /// mayoría de las pasadas no traen nada y no deben gastar dos llamadas de más.
    /// </summary>
    private sealed class ColorResolver
    {
        private readonly IGoogleCalendarClient _calendar;
        private readonly string _token;
        private readonly string _calendarId;
        private readonly ILogger _logger;
        private IReadOnlyDictionary<string, string>? _paleta;
        private string? _colorDelCalendario;
        private bool _cargado;

        public ColorResolver(
            IGoogleCalendarClient calendar, string token, string calendarId, ILogger logger)
        {
            _calendar = calendar;
            _token = token;
            _calendarId = calendarId;
            _logger = logger;
        }

        public async Task<string?> ResolveAsync(string? colorId, CancellationToken cancellationToken)
        {
            await EnsureLoadedAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(colorId) &&
                _paleta is not null &&
                _paleta.TryGetValue(colorId!, out var hex))
            {
                return hex;
            }

            // Sin color propio, el evento se ve con el color del calendario.
            return _colorDelCalendario;
        }

        private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
        {
            if (_cargado)
                return;
            _cargado = true;

            try
            {
                _paleta = await _calendar.GetEventColorsAsync(_token, cancellationToken);
                _colorDelCalendario = await _calendar.GetCalendarColorAsync(
                    _token, _calendarId, cancellationToken);
            }
            catch (GoogleApiException ex)
            {
                // El color es decorativo: que no se pierda una sincronización por él.
                _logger.LogWarning(
                    ex, "No se pudo leer la paleta de colores de Google; se sincroniza sin color.");
            }
        }
    }
}
