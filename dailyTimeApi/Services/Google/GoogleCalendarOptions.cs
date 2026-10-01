namespace dailyTimeApi.Services.Google;

/// <summary>
/// Configuración de la integración con Google Calendar. ClientId y ClientSecret van en
/// user secrets (Google:ClientId / Google:ClientSecret), nunca en appsettings versionados.
/// </summary>
public class GoogleCalendarOptions
{
    public const string SectionName = "Google";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Debe coincidir exactamente con el "URI de redireccionamiento autorizado" del cliente
    /// OAuth en Google Cloud. Google acepta http en loopback (localhost).
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:5100/api/google-calendar/callback";

    public string AuthUrl { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenUrl { get; set; } = "https://oauth2.googleapis.com/token";
    public string RevokeUrl { get; set; } = "https://oauth2.googleapis.com/revoke";
    public string UserInfoUrl { get; set; } = "https://www.googleapis.com/oauth2/v3/userinfo";
    public string ApiBaseUrl { get; set; } = "https://www.googleapis.com/calendar/v3/";

    /// <summary>Cada cuántos minutos corre la sincronización automática.</summary>
    public int SyncIntervalMinutes { get; set; } = 5;

    /// <summary>Zona por defecto de una cuenta recién conectada.</summary>
    public string DefaultTimeZoneId { get; set; } = "America/Bogota";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

/// <summary>Error devuelto por una API de Google (OAuth o Calendar).</summary>
public class GoogleApiException : Exception
{
    public int StatusCode { get; }
    public string Body { get; }

    public GoogleApiException(int statusCode, string body, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Body = body;
    }
}

/// <summary>La cuenta perdió la autorización (refresh token revocado o caducado).</summary>
public class GoogleReauthRequiredException : Exception
{
    public GoogleReauthRequiredException(string message) : base(message) { }
}
