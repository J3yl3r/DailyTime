using System.Collections.Concurrent;
using System.Security.Cryptography;
using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Request;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Repository.Interfaces;
using Microsoft.Extensions.Options;

namespace dailyTimeApi.Services.Google;

public interface IGoogleCalendarService
{
    Task<GoogleCalendarStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    GoogleAuthUrlResponse BuildAuthUrl();
    Task<GoogleCalendarStatusResponse> CompleteConnectAsync(
        string code, string state, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<GoogleCalendarStatusResponse> UpdateSettingsAsync(
        UpdateGoogleCalendarSettingsRequest request, CancellationToken cancellationToken = default);
    Task<GoogleSyncRunResponse> SyncNowAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleCalendarListItemResponse>> ListCalendarsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Vive fuera de la petición: guarda los <c>state</c> de OAuth emitidos hasta que Google
/// devuelve al usuario, para rechazar callbacks que no salieron de aquí.
/// </summary>
public class GoogleOAuthStateStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, DateTime> _issued = new();

    public string Issue()
    {
        Purge();
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _issued[state] = DateTime.UtcNow.Add(Lifetime);
        return state;
    }

    public bool Consume(string? state)
    {
        Purge();
        return !string.IsNullOrWhiteSpace(state) && _issued.TryRemove(state!, out var expiresAt)
            && expiresAt > DateTime.UtcNow;
    }

    private void Purge()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _issued.Where(x => x.Value <= now).ToList())
            _issued.TryRemove(entry.Key, out _);
    }
}

public class GoogleCalendarService : IGoogleCalendarService
{
    private const int MaxWindowDays = 1825;

    private readonly IGoogleCalendarRepository _repository;
    private readonly IGoogleOAuthClient _oauth;
    private readonly IGoogleCalendarClient _calendar;
    private readonly IGoogleCalendarSyncService _sync;
    private readonly GoogleOAuthStateStore _stateStore;
    private readonly GoogleSyncState _syncState;
    private readonly GoogleCalendarOptions _options;

    public GoogleCalendarService(
        IGoogleCalendarRepository repository,
        IGoogleOAuthClient oauth,
        IGoogleCalendarClient calendar,
        IGoogleCalendarSyncService sync,
        GoogleOAuthStateStore stateStore,
        GoogleSyncState syncState,
        IOptions<GoogleCalendarOptions> options)
    {
        _repository = repository;
        _oauth = oauth;
        _calendar = calendar;
        _sync = sync;
        _stateStore = stateStore;
        _syncState = syncState;
        _options = options.Value;
    }

    public async Task<GoogleCalendarStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default) =>
        Map(await _repository.GetAccountAsync(cancellationToken));

    public GoogleAuthUrlResponse BuildAuthUrl()
    {
        EnsureConfigured();
        return new GoogleAuthUrlResponse { AuthUrl = _oauth.BuildAuthUrl(_stateStore.Issue()) };
    }

    public async Task<GoogleCalendarStatusResponse> CompleteConnectAsync(
        string code, string state, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("Google no devolvió el código de autorización.");
        if (!_stateStore.Consume(state))
            throw new ValidationException(
                "La respuesta de Google no corresponde a una conexión iniciada desde aquí. Vuelve a intentarlo.");

        var tokens = await _oauth.ExchangeCodeAsync(code, cancellationToken);
        var email = await _oauth.GetEmailAsync(tokens.AccessToken, cancellationToken) ?? "(desconocido)";
        var now = DateTime.UtcNow;

        var account = await _repository.GetAccountAsync(cancellationToken);
        if (account is null)
        {
            account = new GoogleCalendarAccount
            {
                Email = email,
                CalendarId = "primary",
                TimeZoneId = _options.DefaultTimeZoneId,
                ConnectedAt = now
            };
            await _repository.AddAccountAsync(account, cancellationToken);
        }
        else if (!string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            // Otra cuenta: los eventos de la anterior no existen aquí, así que los elementos
            // vuelven a empezar sin enlace y se recrean en el calendario nuevo.
            await _repository.ClearSyncMarksAsync(cancellationToken);
            account.Email = email;
            account.CalendarId = "primary";
            account.ConnectedAt = now;
            account.PullCutoffAt = null;
        }

        account.AccessToken = tokens.AccessToken;
        account.RefreshToken = tokens.RefreshToken;
        account.AccessTokenExpiresAt = tokens.ExpiresAt;
        account.SyncEnabled = true;
        account.LastSyncStatus = null;
        account.LastSyncMessage = null;
        account.UpdatedAt = now;
        await _repository.SaveChangesAsync(cancellationToken);

        _syncState.RequestRun();
        return Map(account);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountAsync(cancellationToken)
            ?? throw new NotFoundException("No hay ninguna cuenta de Google conectada.");

        // Se revoca el permiso en Google antes de soltar el token; si falla, se desconecta igual.
        try
        {
            await _oauth.RevokeAsync(account.RefreshToken, cancellationToken);
        }
        catch (HttpRequestException) { }
        catch (GoogleApiException) { }

        _repository.RemoveAccount(account);
        await _repository.ClearSyncMarksAsync(cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<GoogleCalendarStatusResponse> UpdateSettingsAsync(
        UpdateGoogleCalendarSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountAsync(cancellationToken)
            ?? throw new NotFoundException("No hay ninguna cuenta de Google conectada.");

        if (request.PastDays < 0 || request.PastDays > MaxWindowDays)
            throw new ValidationException($"Los días hacia atrás deben estar entre 0 y {MaxWindowDays}.");
        if (request.FutureDays < 0 || request.FutureDays > MaxWindowDays)
            throw new ValidationException($"Los días hacia adelante deben estar entre 0 y {MaxWindowDays}.");

        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            var resolved = GoogleEventMapper.ResolveTimeZone(request.TimeZoneId);
            if (resolved.Equals(TimeZoneInfo.Utc) &&
                !string.Equals(request.TimeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Zona horaria desconocida: {request.TimeZoneId}.");
            }
            account.TimeZoneId = request.TimeZoneId!.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.CalendarId) &&
            !string.Equals(request.CalendarId, account.CalendarId, StringComparison.Ordinal))
        {
            // Cambiar de calendario invalida los enlaces: los eventos viven en el anterior.
            await _repository.ClearSyncMarksAsync(cancellationToken);
            account.CalendarId = request.CalendarId!.Trim();
            account.PullCutoffAt = null;
        }

        account.SyncEnabled = request.SyncEnabled;
        account.SyncTimedTasks = request.SyncTimedTasks;
        account.SyncAllDayTasks = request.SyncAllDayTasks;
        account.SyncTimedNotes = request.SyncTimedNotes;
        account.PastDays = request.PastDays;
        account.FutureDays = request.FutureDays;
        account.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);

        if (account.SyncEnabled)
            _syncState.RequestRun();
        return Map(account);
    }

    public async Task<GoogleSyncRunResponse> SyncNowAsync(CancellationToken cancellationToken = default)
    {
        _ = await _repository.GetAccountAsync(cancellationToken)
            ?? throw new NotFoundException("No hay ninguna cuenta de Google conectada.");

        var result = await _sync.SyncAsync(cancellationToken);
        return new GoogleSyncRunResponse
        {
            Pushed = result.Pushed,
            Pulled = result.Pulled,
            Deleted = result.Deleted,
            Message = result.Message
        };
    }

    public async Task<IReadOnlyList<GoogleCalendarListItemResponse>> ListCalendarsAsync(
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetAccountAsync(cancellationToken)
            ?? throw new NotFoundException("No hay ninguna cuenta de Google conectada.");

        if (account.AccessTokenExpiresAt <= DateTime.UtcNow.AddMinutes(2))
        {
            var refreshed = await _oauth.RefreshAsync(account.RefreshToken, cancellationToken);
            account.AccessToken = refreshed.AccessToken;
            account.AccessTokenExpiresAt = refreshed.ExpiresAt;
            account.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        var calendars = await _calendar.ListCalendarsAsync(account.AccessToken, cancellationToken);
        return calendars
            .Select(x => new GoogleCalendarListItemResponse
            {
                Id = x.Id,
                Name = x.Name,
                TimeZoneId = x.TimeZoneId,
                IsPrimary = x.IsPrimary
            })
            .ToList();
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new ValidationException(
                "Faltan las credenciales de Google. Guárdalas en user secrets: " +
                "dotnet user-secrets set \"Google:ClientId\" \"...\" --project dailyTimeApi");
    }

    private GoogleCalendarStatusResponse Map(GoogleCalendarAccount? account) => new()
    {
        Configured = _options.IsConfigured,
        Connected = account is not null,
        Email = account?.Email,
        CalendarId = account?.CalendarId,
        TimeZoneId = account?.TimeZoneId ?? _options.DefaultTimeZoneId,
        SyncEnabled = account?.SyncEnabled ?? false,
        SyncTimedTasks = account?.SyncTimedTasks ?? true,
        SyncAllDayTasks = account?.SyncAllDayTasks ?? true,
        SyncTimedNotes = account?.SyncTimedNotes ?? true,
        PastDays = account?.PastDays ?? 30,
        FutureDays = account?.FutureDays ?? 180,
        LastSyncAt = account?.LastSyncAt,
        LastSyncStatus = account?.LastSyncStatus,
        LastSyncMessage = account?.LastSyncMessage,
        LastPushedCount = account?.LastPushedCount ?? 0,
        LastPulledCount = account?.LastPulledCount ?? 0,
        ConnectedAt = account?.ConnectedAt,
        IsRunning = _syncState.IsRunning,
        SyncIntervalMinutes = Math.Clamp(_options.SyncIntervalMinutes, 1, 1440),
        RedirectUri = _oauth.RedirectUri
    };
}
