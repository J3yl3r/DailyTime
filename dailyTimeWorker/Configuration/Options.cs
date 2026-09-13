namespace dailyTimeWorker.Configuration;

public class DailyTimeApiOptions
{
    public const string SectionName = "DailyTimeApi";
    public string BaseUrl { get; set; } = "https://localhost:5110";
    public bool IgnoreSslErrors { get; set; } = true;
}

public class WorkerOptions
{
    public const string SectionName = "Worker";
    /// <summary>
    /// Interruptor general de la captura programada. Las horas y los portales se configuran
    /// desde la web (horario global en la API); con false el worker solo atiende capturas manuales.
    /// </summary>
    public bool EnableAutoScrape { get; set; } = true;
    public bool UsePlaywright { get; set; } = true;
    public bool Headless { get; set; } = true;
    /// <summary>chrome | msedge | vacío = Chromium de Playwright. Útil si no está instalado el browser del paquete.</summary>
    public string? BrowserChannel { get; set; } = "chrome";
    public string? BrowserExecutablePath { get; set; }
    /// <summary>Retrasa cada acción de Playwright (ms). Útil para ver el bot con Headless=false.</summary>
    public int SlowMoMs { get; set; } = 0;
    /// <summary>Pausa entre URLs (ms).</summary>
    public int DelayBetweenUrlsMs { get; set; } = 800;
    /// <summary>Segundos para esperar un challenge Cloudflare/humano (solo tiene sentido con Headless=false).</summary>
    public int ChallengeWaitSeconds { get; set; } = 180;
    /// <summary>Conecta Playwright a un Chrome ya abierto con remote debugging.</summary>
    public bool UseRemoteDebuggingBrowser { get; set; }
    /// <summary>URL CDP, ej: http://127.0.0.1:9222</summary>
    public string? RemoteDebuggingUrl { get; set; }
    /// <summary>Reutiliza cookies/sesión en un perfil de Chrome (ayuda mucho con Indeed/Cloudflare).</summary>
    public bool UsePersistentProfile { get; set; } = true;
    /// <summary>User Data dir de Chrome/Edge. Vacío = %LocalAppData%\DailyTime\playwright-profile</summary>
    public string? BrowserProfileDir { get; set; }
    /// <summary>Nombre del perfil dentro de User Data, ej: Default, Profile 1.</summary>
    public string? BrowserProfileName { get; set; }
    /// <summary>Clona el User Data a una carpeta temporal antes de lanzar Playwright.</summary>
    public bool CloneBrowserProfile { get; set; } = false;
    /// <summary>Carpeta destino de la copia temporal del perfil. Vacío = %LocalAppData%\DailyTime\playwright-profile-clone</summary>
    public string? BrowserProfileCloneDir { get; set; }
}

public class NotificationsOptions
{
    public const string SectionName = "Notifications";
    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "noreply@dailytime.local";
}
