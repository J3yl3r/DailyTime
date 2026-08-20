using System.Diagnostics;
using dailyTimeWorker.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace dailyTimeWorker.Services.Chrome;

public interface IChromeDebugLauncher
{
    Task<ChromeDebugLaunchResult> LaunchAsync(CancellationToken cancellationToken = default);
    void ScheduleCloseWhenIdle();
}

public sealed class ChromeDebugLaunchResult
{
    public bool Started { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ScriptPath { get; init; }
}

public sealed class ChromeDebugLauncher : IChromeDebugLauncher
{
    private readonly IHostEnvironment _env;
    private readonly WorkerOptions _options;
    private readonly ILogger<ChromeDebugLauncher> _logger;
    private readonly object _gate = new();
    private CancellationTokenSource? _closeCts;

    public ChromeDebugLauncher(
        IHostEnvironment env,
        IOptions<WorkerOptions> options,
        ILogger<ChromeDebugLauncher> logger)
    {
        _env = env;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChromeDebugLaunchResult> LaunchAsync(CancellationToken cancellationToken = default)
    {
        CancelPendingClose();
        var cdpUrl = ResolveCdpUrl();

        if (await IsCdpReadyAsync(cdpUrl, cancellationToken).ConfigureAwait(false))
        {
            return new ChromeDebugLaunchResult
            {
                Started = true,
                Message = $"Chrome debug ya estaba activo en {cdpUrl}."
            };
        }

        var script = ResolveScriptPath("start-chrome-debug.ps1");
        if (script is null)
        {
            return new ChromeDebugLaunchResult
            {
                Started = false,
                Message = "No se encontró scripts/start-chrome-debug.ps1."
            };
        }

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(script) ?? _env.ContentRootPath
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return new ChromeDebugLaunchResult
                {
                    Started = false,
                    ScriptPath = script,
                    Message = "No se pudo iniciar PowerShell para abrir Chrome debug."
                };
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (await IsCdpReadyAsync(cdpUrl, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Chrome debug listo en {CdpUrl} (script {Script})", cdpUrl, script);
                return new ChromeDebugLaunchResult
                {
                    Started = true,
                    ScriptPath = script,
                    Message = $"Chrome debug abierto en {cdpUrl}."
                };
            }

            var stderr = process.ExitCode == 0
                ? null
                : $"El script de Chrome terminó con código {process.ExitCode}.";
            return new ChromeDebugLaunchResult
            {
                Started = false,
                ScriptPath = script,
                Message = stderr
                    ?? $"El script de Chrome terminó pero {cdpUrl} no responde."
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo lanzar Chrome debug.");
            return new ChromeDebugLaunchResult
            {
                Started = false,
                ScriptPath = script,
                Message = ex.Message
            };
        }
    }

    public void ScheduleCloseWhenIdle()
    {
        CancelPendingClose();
        var cts = new CancellationTokenSource();
        lock (_gate)
            _closeCts = cts;

        _logger.LogInformation("Programado cierre de Chrome debug en 3 s (si no hay otra captura).");

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cts.Token).ConfigureAwait(false);
                await CloseDebugAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Cierre de Chrome debug cancelado: hay otra captura en curso.");
            }
        }, CancellationToken.None);
    }

    private void CancelPendingClose()
    {
        lock (_gate)
        {
            try
            {
                _closeCts?.Cancel();
            }
            catch
            {
                /* ignore */
            }
            _closeCts?.Dispose();
            _closeCts = null;
        }
    }

    private async Task CloseDebugAsync(CancellationToken cancellationToken)
    {
        var cdpUrl = ResolveCdpUrl();
        if (!await IsCdpReadyAsync(cdpUrl, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Chrome debug ya estaba cerrado ({CdpUrl}).", cdpUrl);
            return;
        }

        try
        {
            var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            try
            {
                await using var browser = await playwright.Chromium.ConnectOverCDPAsync(cdpUrl)
                    .ConfigureAwait(false);
                await browser.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                playwright.Dispose();
            }

            if (!await IsCdpReadyAsync(cdpUrl, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Chrome debug cerrado vía CDP ({CdpUrl}).", cdpUrl);
                return;
            }

            _logger.LogWarning("Chrome siguió activo tras Browser.CloseAsync; intentando script de cierre.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo cerrar Chrome vía CDP; intentando script.");
        }

        RunStopScript();

        if (await IsCdpReadyAsync(cdpUrl, cancellationToken).ConfigureAwait(false))
            _logger.LogWarning("Chrome debug sigue respondiendo en {CdpUrl} después del cierre.", cdpUrl);
        else
            _logger.LogInformation("Chrome debug cerrado tras la captura.");
    }

    private void RunStopScript()
    {
        var script = ResolveScriptPath("stop-chrome-debug.ps1");
        if (script is null)
        {
            _logger.LogWarning("No se encontró scripts/stop-chrome-debug.ps1.");
            return;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(script) ?? _env.ContentRootPath
            });
            process?.WaitForExit(15000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo ejecutar stop-chrome-debug.ps1.");
        }
    }

    private string ResolveCdpUrl() =>
        string.IsNullOrWhiteSpace(_options.RemoteDebuggingUrl)
            ? "http://127.0.0.1:9222"
            : _options.RemoteDebuggingUrl.TrimEnd('/');

    private static async Task<bool> IsCdpReadyAsync(string cdpUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await client
                .GetAsync($"{cdpUrl}/json/version", cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private string? ResolveScriptPath(string fileName)
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "scripts", fileName)),
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "scripts", fileName)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", fileName)),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
