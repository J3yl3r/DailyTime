using System.Text.Json;
using System.Text.RegularExpressions;
using dailyTimeWorker.Configuration;
using dailyTimeWorker.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace dailyTimeWorker.Services.Scraping;

public interface IPortalScrapeEngine
{
    Task<ScrapeResult> ScrapeAsync(JobPortalDto portal, CancellationToken cancellationToken = default);
}

/// <summary>
/// Motor configurable por portal. Cada sitio tiene su ScrapeConfig JSON.
/// Si cambia el HTML, solo actualizas ese JSON.
/// </summary>
public class PortalScrapeEngine : IPortalScrapeEngine
{
    private readonly WorkerOptions _options;
    private readonly ILogger<PortalScrapeEngine> _logger;

    public PortalScrapeEngine(IOptions<WorkerOptions> options, ILogger<PortalScrapeEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private static readonly HashSet<string> SkippedProfileDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cache",
        "Code Cache",
        "GPUCache",
        "GrShaderCache",
        "ShaderCache",
        "DawnCache",
        "Crashpad",
        "Crash Reports",
        "blob_storage",
        "BrowserMetrics",
        "Component CRX Cache",
        "FileTypePolicies",
        "GraphiteDawnCache",
        "Last Version Temp",
        "Media Cache",
        "optimization_guide_prediction_model_downloads"
    };

    private static readonly HashSet<string> SkippedProfileFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SingletonCookie",
        "SingletonLock",
        "SingletonSocket",
        "DevToolsActivePort",
        "lockfile"
    };

    public async Task<ScrapeResult> ScrapeAsync(
        JobPortalDto portal, CancellationToken cancellationToken = default)
    {
        if (_options.UsePlaywright)
        {
            try
            {
                return await ScrapeWithPlaywrightAsync(portal, cancellationToken);
            }
            catch (Exception ex) when (IsPlaywrightMissing(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Playwright no disponible para {Portal}. Smoke HTTP.",
                    portal.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Playwright en portal {Portal}", portal.Name);
                return Fail(portal, ex.Message);
            }
        }

        return await ScrapeWithHttpSmokeAsync(portal, cancellationToken);
    }

    private async Task<ScrapeResult> ScrapeWithPlaywrightAsync(
        JobPortalDto portal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = ScrapeConfig.Parse(portal.ScrapeConfig);
        var targets = BuildTargetUrls(portal, config);
        _logger.LogInformation(
            "{Portal}: {Count} URL(s) de búsqueda (países×keywords). Ejemplos: {Samples}",
            portal.Name,
            targets.Count,
            string.Join(" | ", targets.Take(3).Select(t => $"{t.CountryCode ?? "?"}: {t.Url}")));

        using var playwright = await Playwright.CreateAsync();
        await using var session = await CreateBrowserSessionAsync(playwright);
        var context = session.Context;
        await ApplyStealthAsync(context);
        await BlockApplyNavigationsAsync(context);

        var page = session.CreateNewPageForScrape
            ? await context.NewPageAsync()
            : context.Pages.LastOrDefault() ?? await context.NewPageAsync();
        try
        {
            await page.BringToFrontAsync();
        }
        catch
        {
            /* ignore */
        }

        // Warm-up del dominio (Indeed) para resolver Cloudflare una vez y reutilizar cookies.
        await WarmUpDomainAsync(page, targets, config, cancellationToken);

        var allOffers = new List<ScrapedOffer>();
        string? lastTitle = null;
        var blockedUrls = 0;
        var loginWallUrls = 0;

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation(
                "Abriendo {Url} (país={Country})",
                target.Url,
                target.CountryName ?? config.DefaultCountry ?? "-");

            await page.GotoAsync(target.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });

            // LinkedIn SPA: espera breve tras la navegación para evitar
            // "Execution context was destroyed" al leer el DOM demasiado pronto.
            try
            {
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await page.WaitForTimeoutAsync(800);
            }
            catch (PlaywrightException ex) when (IsDestroyedContext(ex))
            {
                _logger.LogWarning(ex, "Contexto destruido tras Goto en {Url}; reintentando una vez.", target.Url);
                await page.GotoAsync(target.Url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 60_000
                });
                await page.WaitForTimeoutAsync(1200);
            }

            // LinkedIn a menudo sustituye geoId por la ciudad del perfil; reaplicar país.
            await EnsureLinkedInCountryGeoAsync(page, target);

            var challengePassed = await WaitOutChallengeAsync(page, config, target.Url, cancellationToken);
            if (!challengePassed)
            {
                blockedUrls++;
                _logger.LogWarning(
                    "Challenge Cloudflare/bloqueo no resuelto en {Url}. Se omite esta URL.",
                    target.Url);
                continue;
            }

            if (LooksLikeLoginWall(page.Url))
            {
                loginWallUrls++;
                _logger.LogWarning(
                    "El portal pidió iniciar sesión ({Url}). Inicia sesión en la ventana de Chrome y vuelve a lanzar la captura.",
                    page.Url);
                continue;
            }

            if (LooksLikeComputrabajoCountryGate(page.Url))
            {
                blockedUrls++;
                _logger.LogWarning(
                    "Computrabajo redirigió al selector de países ({FinalUrl}) desde {Url}. " +
                    "El dominio/país ya no sirve (p. ej. computrabajo.es). Se omite esta URL.",
                    page.Url,
                    target.Url);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(config.WaitForSelector))
            {
                try
                {
                    await page.WaitForSelectorAsync(config.WaitForSelector, new PageWaitForSelectorOptions
                    {
                        Timeout = 20_000
                    });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning(
                        "Timeout waitForSelector '{Selector}' en {Url}",
                        config.WaitForSelector, target.Url);
                }
            }

            await ApplyClicksAsync(page, config, target.Url);

            // El cursor debe quedar sobre el listado: LinkedIn carga las tarjetas
            // al hacer scroll dentro del panel, no de la ventana.
            try
            {
                await page.Mouse.MoveAsync(420, 520);
            }
            catch
            {
                /* ignore */
            }

            for (var i = 0; i < config.ScrollTimes; i++)
            {
                await page.Mouse.WheelAsync(0, 1200);
                await page.WaitForTimeoutAsync(Math.Max(400, _options.SlowMoMs));
            }

            lastTitle = await page.TitleAsync();
            try
            {
                var pageOffers = await ExtractOffersAsync(
                    page, portal, config, page.Url, target.CountryName);
                allOffers.AddRange(pageOffers);

                // LinkedIn / Indeed: paginar resultados de BÚSQUEDA (solo tarjetas título/URL).
                // La descripción ("Acerca del empleo") se lee DESPUÉS, oferta por oferta — no aquí.
                if (config.Paginate && IsIndeedUrl(target.Url)
                    && target.Url.Contains("/jobs", StringComparison.OrdinalIgnoreCase))
                {
                    await PaginateIndeedSearchAsync(
                        page, portal, config, target, pageOffers, allOffers, cancellationToken);
                }
                else if (config.Paginate && IsLinkedInUrl(target.Url)
                    && target.Url.Contains("/jobs", StringComparison.OrdinalIgnoreCase))
                {
                    await PaginateLinkedInSearchAsync(
                        page, portal, config, target, pageOffers, allOffers, cancellationToken);
                }
            }
            catch (PlaywrightException ex) when (IsDestroyedContext(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Contexto destruido al extraer en {Url}. Se omite esta URL y se continúa.",
                    target.Url);
            }

            if (_options.DelayBetweenUrlsMs > 0)
                await page.WaitForTimeoutAsync(_options.DelayBetweenUrlsMs);
        }

        var (offers, skippedOld, skippedDup, skippedExcluded, skippedModality) =
            FilterAndDeduplicate(allOffers, config);

        // Indeed: primero detectar candidatas (título/snippet con .NET/React/…);
        // solo esas se abren para descripción — evita enriquecer el listado completo.
        var isIndeedRun = offers.Count > 0 && offers.Any(o => !string.IsNullOrWhiteSpace(o.Url) && IsIndeedUrl(o.Url!));
        if (isIndeedRun
            && (config.RequireContentKeywords.Count > 0 || config.RequireContentKeywordsByCountry.Count > 0))
        {
            var before = offers.Count;
            offers = PreFilterIndeedCandidates(offers, config);
            _logger.LogInformation(
                "Indeed: {Kept}/{Before} candidatas cumplen stack en título/snippet; se enriquecerán solo esas",
                offers.Count,
                before);
        }

        var enriched = 0;
        var linkedInAuthWallHits = 0;
        if (config.VisitDetailPages && offers.Count > 0)
        {
            (enriched, linkedInAuthWallHits) =
                await EnrichDescriptionsAsync(page, offers, config, cancellationToken);
        }

        if (linkedInAuthWallHits > 0)
            loginWallUrls += linkedInAuthWallHits;

        // Filtro de stack: título O descripción/snippet deben tener .NET / React / Next, etc.
        var skippedContent = 0;
        if ((config.RequireContentKeywords.Count > 0 || config.RequireContentKeywordsByCountry.Count > 0)
            && offers.Count > 0)
        {
            var kept = new List<ScrapedOffer>(offers.Count);
            foreach (var offer in offers)
            {
                var title = offer.Title;
                var content = string.Join(" | ",
                    new[] { offer.DescriptionSnippet, offer.Description }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
                var haystack = string.Join(" | ",
                    new[] { title, content }.Where(x => !string.IsNullOrWhiteSpace(x)));

                var require = ResolveRequireContentKeywords(config, offer.Country);
                if (require.Count == 0
                    || OfferFieldNormalizer.MatchesRequiredTech(title, content, require))
                {
                    offer.TechStack ??= OfferFieldNormalizer.DetectTechStack(haystack);
                    kept.Add(offer);
                }
                else
                    skippedContent++;
            }

            offers = kept;
            if (skippedContent > 0)
            {
                _logger.LogInformation(
                    "{Portal}: descartadas {Skipped} ofertas sin señal de stack en título ni contenido",
                    portal.Name,
                    skippedContent);
            }
        }

        // Description: preferible, pero NO descartar si ya pasó el filtro de stack.
        var skippedNoDescription = 0;
        var missingDescription = 0;
        if (config.VisitDetailPages && offers.Count > 0)
        {
            var kept = new List<ScrapedOffer>(offers.Count);
            foreach (var offer in offers)
            {
                var hasDesc = !string.IsNullOrWhiteSpace(offer.Description) && offer.Description.Length >= 40;
                if (hasDesc)
                {
                    var full = string.Join(" | ",
                        new[] { offer.Title, offer.DescriptionSnippet, offer.Description }
                            .Where(x => !string.IsNullOrWhiteSpace(x)));
                    offer.TechStack ??= OfferFieldNormalizer.DetectTechStack(full);
                    kept.Add(offer);
                    continue;
                }

                var softTitle = offer.Title;
                var softContent = offer.DescriptionSnippet;
                var softHaystack = string.Join(" | ",
                    new[] { softTitle, softContent }.Where(x => !string.IsNullOrWhiteSpace(x)));
                var require = ResolveRequireContentKeywords(config, offer.Country);
                if (require.Count == 0
                    || OfferFieldNormalizer.MatchesRequiredTech(softTitle, softContent, require))
                {
                    if (string.IsNullOrWhiteSpace(offer.Description)
                        && !string.IsNullOrWhiteSpace(offer.DescriptionSnippet)
                        && offer.DescriptionSnippet.Length >= 40)
                    {
                        offer.Description = CleanDescription(offer.DescriptionSnippet, 50_000);
                    }

                    offer.TechStack ??= OfferFieldNormalizer.DetectTechStack(softHaystack);
                    missingDescription++;
                    kept.Add(offer);
                    continue;
                }

                skippedNoDescription++;
            }

            offers = kept;
            if (missingDescription > 0)
            {
                _logger.LogWarning(
                    "{Portal}: {Count} ofertas con stack válido sin Description completa (posible authwall/login). Se conservan por título/snippet.",
                    portal.Name,
                    missingDescription);
            }

            if (skippedNoDescription > 0)
            {
                _logger.LogInformation(
                    "{Portal}: descartadas {Skipped} ofertas sin Description ni señal de stack",
                    portal.Name,
                    skippedNoDescription);
            }
        }

        var parts = new List<string> { $"{offers.Count} ofertas" };
        if (enriched > 0)
            parts.Add($"{enriched} con descripción de detalle");
        if (skippedOld > 0)
            parts.Add($"{skippedOld} fuera de antigüedad");
        if (skippedDup > 0)
            parts.Add($"{skippedDup} duplicadas");
        if (skippedExcluded > 0)
            parts.Add($"{skippedExcluded} exclusión (inclusión/discapacidad)");
        if (skippedModality > 0)
            parts.Add($"{skippedModality} sin remoto/híbrido (ES/MX)");
        if (missingDescription > 0)
            parts.Add($"{missingDescription} .NET sin Description completa");
        if (skippedNoDescription > 0)
            parts.Add($"{skippedNoDescription} sin Description");
        if (skippedContent > 0)
            parts.Add($"{skippedContent} sin señal .NET/contenido");
        if (blockedUrls > 0)
            parts.Add($"{blockedUrls} URL(s) bloqueadas por challenge");
        if (loginWallUrls > 0)
            parts.Add($"{loginWallUrls} URL(s) pidieron login/authwall");
        parts.Add($"{targets.Count} URL(s)");

        var ageLabel = config.MaxAgeHours is int h
            ? $"maxAgeHours={h}"
            : config.MaxAgeDays is int d
                ? $"maxAgeDays={d}"
                : "antigüedad";

        return new ScrapeResult
        {
            PortalId = portal.Id,
            PortalName = portal.Name,
            Status = offers.Count > 0 ? "ok" : blockedUrls + loginWallUrls > 0 ? "blocked" : "no_offers",
            Message = offers.Count > 0
                ? $"Se extrajeron {string.Join(", ", parts)}."
                : loginWallUrls > 0
                    ? $"{portal.Name} pidió iniciar sesión en {loginWallUrls} URL(s). Inicia sesión en la ventana de Chrome (perfil debug) y relanza la captura."
                    : blockedUrls > 0
                    ? $"El portal bloqueó la captura con un challenge ({blockedUrls} URL(s)). Con Headless=false marca el checkbox 'Verifique que es un ser humano' en la ventana de Chrome (tienes ~{_options.ChallengeWaitSeconds}s)."
                    : skippedOld > 0
                        ? $"Sin ofertas recientes (descartadas {skippedOld} por {ageLabel})."
                        : skippedModality > 0
                            ? $"Sin ofertas remoto/híbrido para ES/MX (descartadas {skippedModality})."
                        : "Sin ofertas. Revisa ScrapeConfig (listSelectors / searchKeywords).",
            PageTitle = lastTitle,
            Offers = offers
        };
    }

    private sealed class BrowserSession : IAsyncDisposable
    {
        public required IBrowserContext Context { get; init; }
        public IBrowser? Browser { get; init; }
        public bool DisposeContext { get; init; } = true;
        public bool DisposeBrowser { get; init; } = true;
        public bool CreateNewPageForScrape { get; init; }

        public async ValueTask DisposeAsync()
        {
            if (DisposeContext)
                await Context.DisposeAsync();
            if (DisposeBrowser && Browser is not null)
                await Browser.DisposeAsync();
        }
    }

    private async Task<BrowserSession> CreateBrowserSessionAsync(IPlaywright playwright)
    {
        if (_options.UseRemoteDebuggingBrowser && !string.IsNullOrWhiteSpace(_options.RemoteDebuggingUrl))
        {
            var cdpBrowser = await ConnectToExistingBrowserAsync(playwright);
            var context = cdpBrowser.Contexts.FirstOrDefault();
            if (context is null)
                throw new PlaywrightException(
                    "Chrome por remote debugging no expuso ningún contexto. Ábrelo con --remote-debugging-port=9222 y deja una ventana abierta.");

            return new BrowserSession
            {
                Browser = cdpBrowser,
                Context = context,
                DisposeBrowser = false,
                DisposeContext = false,
                CreateNewPageForScrape = true
            };
        }

        if (_options.UsePersistentProfile)
        {
            var profileDir = PrepareBrowserProfileLaunchDir();
            Directory.CreateDirectory(profileDir);
            _logger.LogInformation("Usando perfil persistente Playwright: {Dir}", profileDir);

            var attempts = BuildPersistentLaunchAttempts(profileDir);
            Exception? last = null;
            foreach (var attempt in attempts)
            {
                try
                {
                    _logger.LogInformation(
                        "Lanzando contexto persistente Headless={Headless} Channel={Channel}",
                        _options.Headless,
                        attempt.Channel ?? "(chromium)");
                    var context = await playwright.Chromium.LaunchPersistentContextAsync(profileDir, attempt);
                    return new BrowserSession { Context = context, Browser = null };
                }
                catch (Exception ex) when (IsPlaywrightMissing(ex) || ex is PlaywrightException)
                {
                    last = ex;
                    _logger.LogWarning(ex, "No se pudo lanzar contexto persistente Channel={Channel}", attempt.Channel);
                }
            }

            throw last ?? new PlaywrightException("No se pudo lanzar contexto persistente.");
        }

        var browser = await LaunchBrowserAsync(playwright);
        var context2 = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "es-CO",
            TimezoneId = "America/Bogota",
            ViewportSize = new ViewportSize { Width = 1365, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        return new BrowserSession { Context = context2, Browser = browser };
    }

    private async Task<IBrowser> ConnectToExistingBrowserAsync(IPlaywright playwright)
    {
        var endpoint = _options.RemoteDebuggingUrl!.Trim();
        _logger.LogInformation("Conectando a Chrome existente por CDP: {Endpoint}", endpoint);
        return await playwright.Chromium.ConnectOverCDPAsync(endpoint);
    }

    private string ResolveBrowserProfileSourceDir()
    {
        if (!string.IsNullOrWhiteSpace(_options.BrowserProfileDir))
            return _options.BrowserProfileDir;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyTime",
            "playwright-profile");
    }

    private string PrepareBrowserProfileLaunchDir()
    {
        var sourceDir = ResolveBrowserProfileSourceDir();
        if (!_options.CloneBrowserProfile || string.IsNullOrWhiteSpace(_options.BrowserProfileDir))
            return sourceDir;

        var cloneDir = ResolveBrowserProfileCloneDir();
        var selectedProfile = string.IsNullOrWhiteSpace(_options.BrowserProfileName)
            ? "Default"
            : _options.BrowserProfileName.Trim();

        if (IsReusableProfileCloneReady(cloneDir, selectedProfile))
        {
            _logger.LogInformation("Reutilizando clon persistente de perfil Playwright: {Clone}", cloneDir);
            return cloneDir;
        }

        CloneChromeUserDataDir(sourceDir, cloneDir, selectedProfile);
        _logger.LogInformation("Perfil clonado para Playwright: {Source} -> {Clone}", sourceDir, cloneDir);
        return cloneDir;
    }

    private string ResolveBrowserProfileCloneDir()
    {
        if (!string.IsNullOrWhiteSpace(_options.BrowserProfileCloneDir))
            return _options.BrowserProfileCloneDir;
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyTime",
            "playwright-profile-clone");
    }

    private bool IsReusableProfileCloneReady(string cloneDir, string profileName)
    {
        return Directory.Exists(cloneDir)
               && File.Exists(Path.Combine(cloneDir, "Local State"))
               && Directory.Exists(Path.Combine(cloneDir, profileName));
    }

    private void CloneChromeUserDataDir(string sourceDir, string cloneDir, string profileName)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"No existe el perfil fuente: {sourceDir}");

        if (Directory.Exists(cloneDir))
            Directory.Delete(cloneDir, recursive: true);
        Directory.CreateDirectory(cloneDir);

        foreach (var rootFile in new[] { "Local State", "First Run", "Last Version" })
        {
            var sourceFile = Path.Combine(sourceDir, rootFile);
            var destFile = Path.Combine(cloneDir, rootFile);
            TryCopyFile(sourceFile, destFile);
        }

        var sourceProfileDir = Path.Combine(sourceDir, profileName);
        var destProfileDir = Path.Combine(cloneDir, profileName);
        CopyDirectoryRecursive(sourceProfileDir, destProfileDir);
    }

    private void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"No existe el directorio de perfil a copiar: {sourceDir}");

        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            if (SkippedProfileFiles.Contains(fileName))
                continue;

            var destFile = Path.Combine(destDir, fileName);
            TryCopyFile(file, destFile);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            if (SkippedProfileDirectories.Contains(dirName))
                continue;

            CopyDirectoryRecursive(dir, Path.Combine(destDir, dirName));
        }
    }

    private void TryCopyFile(string sourceFile, string destFile)
    {
        if (!File.Exists(sourceFile))
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(sourceFile, destFile, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo copiar archivo de perfil {File}", sourceFile);
        }
    }

    private List<BrowserTypeLaunchPersistentContextOptions> BuildPersistentLaunchAttempts(string profileDir)
    {
        var profileName = _options.BrowserProfileName?.Trim();
        var hasExplicitBrowser = !string.IsNullOrWhiteSpace(_options.BrowserExecutablePath)
            || !string.IsNullOrWhiteSpace(_options.BrowserChannel);
        BrowserTypeLaunchPersistentContextOptions Base()
        {
            var opt = new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = _options.Headless,
                ChromiumSandbox = true,
                SlowMo = Math.Max(0, _options.SlowMoMs),
                Locale = "es-CO",
                TimezoneId = "America/Bogota",
                ViewportSize = new ViewportSize { Width = 1365, Height = 900 },
                IgnoreHTTPSErrors = true,
                IgnoreDefaultArgs = ["--enable-automation"],
                Args = string.IsNullOrWhiteSpace(profileName)
                    ? [
                        "--disable-infobars",
                        "--no-first-run",
                        "--no-default-browser-check"
                    ]
                    : [
                        "--disable-infobars",
                        "--no-first-run",
                        "--no-default-browser-check",
                        $"--profile-directory={profileName}"
                    ]
            };

            return opt;
        }

        var list = new List<BrowserTypeLaunchPersistentContextOptions>();

        if (!string.IsNullOrWhiteSpace(_options.BrowserExecutablePath))
        {
            var opt = Base();
            opt.ExecutablePath = _options.BrowserExecutablePath;
            list.Add(opt);
        }

        if (!string.IsNullOrWhiteSpace(_options.BrowserChannel))
        {
            var opt = Base();
            opt.Channel = _options.BrowserChannel;
            list.Add(opt);
        }

        if (hasExplicitBrowser)
            return list;

        list.Add(Base());

        foreach (var channel in new[] { "chrome", "msedge" })
        {
            if (string.Equals(_options.BrowserChannel, channel, StringComparison.OrdinalIgnoreCase))
                continue;
            var opt = Base();
            opt.Channel = channel;
            list.Add(opt);
        }

        return list;
    }

    private static async Task ApplyStealthAsync(IBrowserContext context)
    {
        await context.AddInitScriptAsync(
            """
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            window.chrome = window.chrome || { runtime: {} };
            Object.defineProperty(navigator, 'languages', { get: () => ['es-CO', 'es', 'en-US', 'en'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            """);
    }

    private async Task BlockApplyNavigationsAsync(IBrowserContext context)
    {
        // Importante: NO interceptar "**/*". Eso rompe Cloudflare Turnstile.
        foreach (var pattern in new[]
                 {
                     "**/*indeedapply*",
                     "**/*indeed-apply*",
                     "**/*smartapply*",
                     "**/*/apply*",
                     "**/*/aplicar*",
                     "**/*/postular*"
                 })
        {
            await context.RouteAsync(pattern, async route =>
            {
                var url = route.Request.Url;
                if (LooksLikeApplyUrl(url))
                {
                    _logger.LogInformation("Bloqueada navegación de postulación: {Url}", url);
                    await route.AbortAsync();
                    return;
                }

                await route.ContinueAsync();
            });
        }
    }

    private async Task WarmUpDomainAsync(
        IPage page,
        IReadOnlyList<ScrapeTarget> targets,
        ScrapeConfig config,
        CancellationToken cancellationToken)
    {
        var firstUrl = targets.FirstOrDefault()?.Url;
        if (string.IsNullOrWhiteSpace(firstUrl))
            return;
        if (!firstUrl.Contains("indeed.", StringComparison.OrdinalIgnoreCase)
            && !firstUrl.Contains("indeed.com", StringComparison.OrdinalIgnoreCase))
            return;

        if (!Uri.TryCreate(firstUrl, UriKind.Absolute, out var uri))
            return;

        var home = $"{uri.Scheme}://{uri.Host}/";
        _logger.LogInformation("Warm-up dominio Indeed: {Home}", home);
        try
        {
            await page.GotoAsync(home, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await WaitOutChallengeAsync(page, config, home, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Warm-up Indeed falló en {Home}", home);
        }
    }

    private sealed record ScrapeTarget(string Url, string? CountryName, string? CountryCode);

    private static IReadOnlyList<ScrapeTarget> BuildTargetUrls(JobPortalDto portal, ScrapeConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SearchUrlTemplate))
            return [new ScrapeTarget(portal.Url, config.DefaultCountry, null)];

        var needsKeyword = config.SearchUrlTemplate.Contains("{query}", StringComparison.OrdinalIgnoreCase)
            || config.SearchUrlTemplate.Contains("{keyword}", StringComparison.OrdinalIgnoreCase);

        if (needsKeyword && config.SearchKeywords.Count == 0 && config.SearchKeywordsByCountry.Count == 0)
            return [new ScrapeTarget(portal.Url, config.DefaultCountry, null)];

        var countries = config.SearchCountries.Count > 0
            ? config.SearchCountries
            : [new SearchCountry { Code = "", Name = config.DefaultCountry ?? "" }];

        var targets = new List<ScrapeTarget>();
        foreach (var country in countries)
        {
            var keywords = ResolveSearchKeywords(config, country);
            if (keywords.Count == 0 && needsKeyword)
                continue;
            if (keywords.Count == 0)
                keywords = [""];

            foreach (var keyword in keywords)
            {
                var slug = string.IsNullOrWhiteSpace(keyword) ? "" : Slugify(keyword);
                var query = string.IsNullOrWhiteSpace(keyword) ? "" : Uri.EscapeDataString(keyword);
                var host = string.IsNullOrWhiteSpace(country.Host)
                    ? ""
                    : country.Host.Trim();
                var locationText = !string.IsNullOrWhiteSpace(country.Location)
                    ? country.Location!
                    : country.Name ?? "";
                var geoId = country.GeoId?.Trim() ?? "";
                var url = config.SearchUrlTemplate!
                    .Replace("{countryCode}", country.Code ?? "", StringComparison.OrdinalIgnoreCase)
                    .Replace("{location}", Uri.EscapeDataString(locationText), StringComparison.OrdinalIgnoreCase)
                    .Replace("{country}", Uri.EscapeDataString(locationText), StringComparison.OrdinalIgnoreCase)
                    .Replace("{geoId}", geoId, StringComparison.OrdinalIgnoreCase)
                    .Replace("{host}", host, StringComparison.OrdinalIgnoreCase)
                    .Replace("{keyword}", slug, StringComparison.OrdinalIgnoreCase)
                    .Replace("{query}", query, StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(url))
                    continue;

                // Quitar geoId vacío para no romper el querystring.
                if (string.IsNullOrWhiteSpace(geoId))
                {
                    url = url
                        .Replace("geoId=&", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("?geoId=&", "?", StringComparison.OrdinalIgnoreCase)
                        .Replace("&geoId=", "", StringComparison.OrdinalIgnoreCase);
                    if (url.EndsWith("?geoId=", StringComparison.OrdinalIgnoreCase)
                        || url.EndsWith("&geoId=", StringComparison.OrdinalIgnoreCase))
                        url = url[..^"geoId=".Length].TrimEnd('?', '&');
                }

                url = ApplyCountryUrlFilters(url, country, config);

                targets.Add(new ScrapeTarget(
                    url,
                    string.IsNullOrWhiteSpace(country.Name) ? config.DefaultCountry : country.Name,
                    string.IsNullOrWhiteSpace(country.Code) ? null : country.Code));
            }
        }

        if (targets.Count == 0)
            return [new ScrapeTarget(portal.Url, config.DefaultCountry, null)];

        return targets
            .GroupBy(t => t.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static IReadOnlyList<string> ResolveSearchKeywords(ScrapeConfig config, SearchCountry country)
    {
        var set = new List<string>();
        void AddRange(IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;
                if (set.Any(x => string.Equals(x, item.Trim(), StringComparison.OrdinalIgnoreCase)))
                    continue;
                set.Add(item.Trim());
            }
        }

        AddRange(config.SearchKeywords);
        AddRange(LookupCountryKeywordMap(config.SearchKeywordsByCountry, country.Code, country.Name));
        return set;
    }

    private static IReadOnlyList<string> ResolveRequireContentKeywords(ScrapeConfig config, string? countryNameOrCode)
    {
        var set = new List<string>();
        void AddRange(IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;
                if (set.Any(x => string.Equals(x, item.Trim(), StringComparison.OrdinalIgnoreCase)))
                    continue;
                set.Add(item.Trim());
            }
        }

        AddRange(config.RequireContentKeywords);
        AddRange(LookupCountryKeywordMap(config.RequireContentKeywordsByCountry, countryNameOrCode, countryNameOrCode));
        return set;
    }

    private static IReadOnlyList<string> LookupCountryKeywordMap(
        IReadOnlyDictionary<string, IReadOnlyList<string>> map,
        string? code,
        string? name)
    {
        if (map.Count == 0)
            return [];

        foreach (var key in CountryLookupKeys(code, name))
        {
            if (map.TryGetValue(key, out var list) && list.Count > 0)
                return list;
        }

        return [];
    }

    private static IEnumerable<string> CountryLookupKeys(string? code, string? name)
    {
        var c = (code ?? "").Trim().ToLowerInvariant();
        var n = (name ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(c))
            yield return c;
        if (!string.IsNullOrEmpty(n))
            yield return n;
        if (n.Contains("méxico", StringComparison.Ordinal) || n.Contains("mexico", StringComparison.Ordinal))
            yield return "mx";
        if (n.Contains("españa", StringComparison.Ordinal) || n.Contains("espana", StringComparison.Ordinal)
            || n.Contains("spain", StringComparison.Ordinal))
            yield return "es";
        if (n.Contains("colombia", StringComparison.Ordinal))
            yield return "co";
    }

    /// <summary>
    /// Ajustes por país en la URL (ej. LinkedIn: solo remoto/híbrido en ES/MX).
    /// </summary>
    private static string ApplyCountryUrlFilters(string url, SearchCountry country, ScrapeConfig config)
    {
        if (!RequiresRemoteOrHybrid(country.Code, country.Name, config))
            return url;

        // LinkedIn workplace type: 2=Remote, 3=Hybrid
        if (url.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("f_WT=", StringComparison.OrdinalIgnoreCase))
        {
            url += (url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "f_WT=2%2C3";
        }

        return url;
    }

    private static bool RequiresRemoteOrHybrid(string? countryCode, string? countryName, ScrapeConfig config)
    {
        if (config.RemoteOrHybridOnlyCountryCodes.Count == 0)
            return false;

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var code = (countryCode ?? "").Trim().ToLowerInvariant();
        var name = (countryName ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(code))
            keys.Add(code);
        if (!string.IsNullOrEmpty(name))
            keys.Add(name);

        // Alias comunes para comparar contra códigos cortos.
        if (name.Contains("méxico", StringComparison.Ordinal) || name.Contains("mexico", StringComparison.Ordinal))
            keys.Add("mx");
        if (name.Contains("españa", StringComparison.Ordinal) || name.Contains("espana", StringComparison.Ordinal)
            || name.Contains("spain", StringComparison.Ordinal))
            keys.Add("es");
        if (name.Contains("colombia", StringComparison.Ordinal))
            keys.Add("co");

        foreach (var raw in config.RemoteOrHybridOnlyCountryCodes)
        {
            var token = (raw ?? "").Trim().ToLowerInvariant();
            if (token.Length == 0)
                continue;
            if (keys.Contains(token))
                return true;
        }

        return false;
    }

    private static bool IsRemoteOrHybridModality(string? modality, ScrapedOffer offer)
    {
        if (string.Equals(modality, "Remoto", StringComparison.OrdinalIgnoreCase)
            || string.Equals(modality, "Híbrido", StringComparison.OrdinalIgnoreCase)
            || string.Equals(modality, "Hibrido", StringComparison.OrdinalIgnoreCase))
            return true;

        // Fallback: el texto de la tarjeta/ubicación puede decirlo aunque no se normalizó.
        var haystack = string.Join(" | ",
            new[] { offer.WorkModality, offer.Location, offer.Title, offer.DescriptionSnippet }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        var guessed = OfferFieldNormalizer.NormalizeModality(haystack);
        return string.Equals(guessed, "Remoto", StringComparison.OrdinalIgnoreCase)
               || string.Equals(guessed, "Híbrido", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
    {
        var attempts = BuildLaunchAttempts();
        Exception? last = null;

        foreach (var attempt in attempts)
        {
            try
            {
                _logger.LogInformation(
                    "Lanzando browser Headless={Headless} Channel={Channel} Executable={Exe}",
                    _options.Headless,
                    attempt.Channel ?? "(chromium)",
                    attempt.ExecutablePath ?? "(default)");

                return await playwright.Chromium.LaunchAsync(attempt);
            }
            catch (Exception ex) when (IsPlaywrightMissing(ex) || ex is PlaywrightException)
            {
                last = ex;
                _logger.LogWarning(ex, "No se pudo lanzar browser con Channel={Channel}", attempt.Channel);
            }
        }

        throw last ?? new PlaywrightException("No se pudo lanzar ningún browser (chromium/chrome/msedge).");
    }

    private List<BrowserTypeLaunchOptions> BuildLaunchAttempts()
    {
        BrowserTypeLaunchOptions Base() => new()
        {
            Headless = _options.Headless,
            ChromiumSandbox = true,
            SlowMo = Math.Max(0, _options.SlowMoMs),
            IgnoreDefaultArgs = ["--enable-automation"],
            Args =
            [
                "--disable-infobars",
                "--no-first-run",
                "--no-default-browser-check"
            ]
        };

        var list = new List<BrowserTypeLaunchOptions>();

        if (!string.IsNullOrWhiteSpace(_options.BrowserExecutablePath))
        {
            var opt = Base();
            opt.ExecutablePath = _options.BrowserExecutablePath;
            list.Add(opt);
        }

        if (!string.IsNullOrWhiteSpace(_options.BrowserChannel))
        {
            var opt = Base();
            opt.Channel = _options.BrowserChannel;
            list.Add(opt);
        }

        list.Add(Base());

        foreach (var channel in new[] { "chrome", "msedge" })
        {
            if (string.Equals(_options.BrowserChannel, channel, StringComparison.OrdinalIgnoreCase))
                continue;
            var opt = Base();
            opt.Channel = channel;
            list.Add(opt);
        }

        return list;
    }

    private async Task<bool> WaitOutChallengeAsync(
        IPage page, ScrapeConfig config, string url, CancellationToken cancellationToken)
    {
        if (!await LooksLikeChallengeAsync(page))
            return true;

        if (_options.Headless)
        {
            _logger.LogWarning(
                "Challenge Cloudflare en {Url} con Headless=true. Cambia Worker:Headless=false y marca el checkbox.",
                url);
            return false;
        }

        var waitSeconds = Math.Max(30, _options.ChallengeWaitSeconds);
        _logger.LogWarning(
            "Challenge Cloudflare en {Url}. Trae la ventana al frente, marca 'Verifique que es un ser humano' y espera (hasta {Seconds}s).",
            url, waitSeconds);

        try
        {
            await page.BringToFrontAsync();
        }
        catch
        {
            /* ignore */
        }

        var deadline = DateTime.UtcNow.AddSeconds(waitSeconds);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await LooksLikeChallengeAsync(page))
            {
                // Dar margen a Indeed/Cloudflare para completar la redirección final.
                await page.WaitForTimeoutAsync(3_000);

                if (!string.IsNullOrWhiteSpace(config.WaitForSelector))
                {
                    try
                    {
                        await page.WaitForSelectorAsync(config.WaitForSelector, new PageWaitForSelectorOptions
                        {
                            Timeout = 10_000
                        });
                        return true;
                    }
                    catch (TimeoutException)
                    {
                        // Puede estar cargando aún tras el challenge.
                    }
                }
                else
                {
                    return true;
                }
            }

            await page.WaitForTimeoutAsync(1_000);
        }

        return !await LooksLikeChallengeAsync(page);
    }

    private static async Task<bool> LooksLikeChallengeAsync(IPage page)
    {
        var title = (await page.TitleAsync()) ?? string.Empty;
        if (title.Contains("Security Check", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Attention Required", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Verificación adicional", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var challengeDom = await page.Locator(
                "iframe[src*='challenges.cloudflare.com'], iframe[src*='turnstile'], #challenge-form, .cf-turnstile")
                .CountAsync();
            if (challengeDom > 0)
            {
                // Si además hay listado de jobs, no es challenge (widget residual).
                var jobs = await page.Locator("div.job_seen_beacon, .jobsearch-ResultsList, a[data-jk]").CountAsync();
                if (jobs == 0)
                    return true;
            }
        }
        catch
        {
            /* ignore */
        }

        var bodyText = string.Empty;
        try
        {
            bodyText = await page.InnerTextAsync("body", new PageInnerTextOptions { Timeout = 2_000 });
        }
        catch
        {
            /* ignore */
        }

        return bodyText.Contains("Verificación adicional requerida", StringComparison.OrdinalIgnoreCase)
               || bodyText.Contains("Verifique que es un ser humano", StringComparison.OrdinalIgnoreCase)
               || bodyText.Contains("Checking your browser", StringComparison.OrdinalIgnoreCase)
               || bodyText.Contains("Perform a security check", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeLoginWall(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.Contains("/authwall", StringComparison.OrdinalIgnoreCase)
               || url.Contains("/checkpoint/", StringComparison.OrdinalIgnoreCase)
               || url.Contains("/uas/login", StringComparison.OrdinalIgnoreCase)
               || url.Contains("linkedin.com/login", StringComparison.OrdinalIgnoreCase)
               || url.Contains("session_redirect", StringComparison.OrdinalIgnoreCase)
               || url.Contains("indeed.com/account/login", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> LooksLikeLinkedInAuthWallPageAsync(IPage page)
    {
        try
        {
            if (LooksLikeLoginWall(page.Url))
                return true;

            return await page.EvaluateAsync<bool>(
                """
                () => {
                  const t = (document.body && (document.body.innerText || '')) || '';
                  const title = (document.title || '').toLowerCase();
                  if (/authwall/i.test(location.href)) return true;
                  if (/únete a linkedin|unete a linkedin|join linkedin|sign in|inicia sesi[oó]n/i.test(t.slice(0, 800)))
                    return true;
                  if (/aceptar y unirse|accept and join/i.test(t.slice(0, 800))) return true;
                  if (title.includes('sign up') || title.includes('join linkedin') || title.includes('authwall'))
                    return true;
                  return false;
                }
                """);
        }
        catch
        {
            return LooksLikeLoginWall(page.Url);
        }
    }

    /// <summary>
    /// computrabajo.es (y dominios muertos) redirigen a www.computrabajo.com (selector de banderas).
    /// </summary>
    private static bool LooksLikeComputrabajoCountryGate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        if (host is not ("computrabajo.com" or "www.computrabajo.com"))
            return false;

        // Solo la home / selector de país (sin path de ofertas).
        var path = uri.AbsolutePath.TrimEnd('/');
        return path.Length == 0 || path.Equals("/", StringComparison.Ordinal);
    }

    private static bool LooksLikeApplyUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        return url.Contains("indeedapply", StringComparison.OrdinalIgnoreCase)
               || url.Contains("indeed-apply", StringComparison.OrdinalIgnoreCase)
               || url.Contains("/apply", StringComparison.OrdinalIgnoreCase)
               || url.Contains("smartapply", StringComparison.OrdinalIgnoreCase)
               || url.Contains("postular", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeApplySelector(string selector)
    {
        var s = selector.ToLowerInvariant();
        return s.Contains("apply")
               || s.Contains("postular")
               || s.Contains("indeedapply")
               || s.Contains("indeed-apply");
    }

    private static string Slugify(string value)
    {
        var text = value.Trim().ToLowerInvariant();
        // c# → csharp (si no, queda "c" y en Computrabajo trae conductores C2, etc.)
        text = text.Replace("c#", "csharp", StringComparison.Ordinal);
        text = text.Replace("#", "sharp", StringComparison.Ordinal);
        text = text.Replace(".net", "net", StringComparison.Ordinal);
        text = Regex.Replace(text, @"[^a-z0-9]+", "-");
        return text.Trim('-');
    }

    private async Task ApplyClicksAsync(IPage page, ScrapeConfig config, string url)
    {
        foreach (var selector in config.ClickBeforeExtract)
        {
            if (LooksLikeApplySelector(selector))
            {
                _logger.LogWarning(
                    "Se omitió clickBeforeExtract '{Selector}' porque parece flujo de postulación.",
                    selector);
                continue;
            }

            try
            {
                var locator = page.Locator(selector);
                var count = await locator.CountAsync();
                if (count == 0)
                {
                    _logger.LogWarning(
                        "clickBeforeExtract no encontró '{Selector}' en {Url}",
                        selector, url);
                    continue;
                }

                // Preferir el primer visible (desktop vs mobile).
                ILocator target = locator.First;
                for (var i = 0; i < count; i++)
                {
                    var candidate = locator.Nth(i);
                    if (await candidate.IsVisibleAsync())
                    {
                        target = candidate;
                        break;
                    }
                }

                _logger.LogInformation("Click '{Selector}' en {Url}", selector, url);
                await target.ClickAsync(new LocatorClickOptions { Timeout = 10_000 });
                await page.WaitForTimeoutAsync(Math.Max(300, config.WaitAfterClickMs));

                if (!string.IsNullOrWhiteSpace(config.WaitForSelector))
                {
                    try
                    {
                        await page.WaitForSelectorAsync(config.WaitForSelector, new PageWaitForSelectorOptions
                        {
                            Timeout = 15_000
                        });
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning(
                            "Timeout waitForSelector tras click '{Selector}'",
                            selector);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falló clickBeforeExtract '{Selector}'", selector);
            }
        }
    }

    private static (List<ScrapedOffer> Offers, int SkippedOld, int SkippedDup, int SkippedExcluded, int SkippedModality)
        FilterAndDeduplicate(IReadOnlyList<ScrapedOffer> raw, ScrapeConfig config)
    {
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenContent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var offers = new List<ScrapedOffer>();
        var skippedOld = 0;
        var skippedDup = 0;
        var skippedExcluded = 0;
        var skippedModality = 0;
        var now = DateTime.UtcNow;

        foreach (var offer in raw)
        {
            var exclusionText = string.Join(" | ",
                new[] { offer.Title, offer.Company, offer.Location, offer.DescriptionSnippet, offer.Description, offer.PostedAtText }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (OfferFieldNormalizer.MatchesExclusion(exclusionText, config.ExcludeKeywords))
            {
                skippedExcluded++;
                continue;
            }

            if (RequiresRemoteOrHybrid(null, offer.Country, config)
                && !IsRemoteOrHybridModality(offer.WorkModality, offer))
            {
                skippedModality++;
                continue;
            }

            if (config.MaxAgeHours is int maxHours)
            {
                var ageHours = OfferDateParser.TryParseAgeHours(offer.PostedAtText);
                if (ageHours is null && offer.PostedAt is DateTime postedAt)
                    ageHours = Math.Max(0, (now - postedAt.ToUniversalTime()).TotalHours);

                if (ageHours is null)
                {
                    if (config.DropIfDateUnknown)
                    {
                        skippedOld++;
                        continue;
                    }
                }
                else if (ageHours.Value > maxHours)
                {
                    skippedOld++;
                    continue;
                }
            }
            else if (config.MaxAgeDays is int maxAge)
            {
                var age = OfferDateParser.TryParseAgeDays(offer.PostedAtText);
                if (age is null && offer.PostedAt is DateTime postedAt)
                    age = Math.Max(0, (int)(now.Date - postedAt.ToUniversalTime().Date).TotalDays);

                if (age is null)
                {
                    if (config.DropIfDateUnknown)
                    {
                        skippedOld++;
                        continue;
                    }
                }
                else if (age.Value > maxAge)
                {
                    skippedOld++;
                    continue;
                }
            }

            var urlKey = NormalizeOfferUrl(offer.Url);
            var contentKey =
                $"{NormalizeKeyPart(offer.Title)}|{NormalizeKeyPart(offer.Company)}|{NormalizeKeyPart(offer.Country)}";
            // Prefer normalized URL; never keep a known-broken Indeed pagead/clk without jk.
            offer.Url = urlKey;
            offer.ExternalKey = urlKey ?? contentKey;

            if (urlKey is not null && seenUrls.Contains(urlKey))
            {
                skippedDup++;
                continue;
            }

            if (seenContent.Contains(contentKey))
            {
                skippedDup++;
                continue;
            }

            if (urlKey is not null)
                seenUrls.Add(urlKey);
            seenContent.Add(contentKey);
            offers.Add(offer);
        }

        // Más recientes primero.
        offers.Sort((a, b) =>
        {
            var aDate = a.PostedAt ?? DateTime.MinValue;
            var bDate = b.PostedAt ?? DateTime.MinValue;
            var cmp = bDate.CompareTo(aDate);
            return cmp != 0 ? cmp : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        return (offers, skippedOld, skippedDup, skippedExcluded, skippedModality);
    }

    private static string? NormalizeOfferUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return url.Trim();

        var host = uri.Host.ToLowerInvariant();

        // Indeed ads use /pagead/clk?jk=... — without the query the link 404s.
        // Prefer the stable public job URL: /viewjob?jk=...
        if (IsIndeedHost(host))
        {
            var jk = GetQueryParam(uri, "jk");
            if (!string.IsNullOrWhiteSpace(jk))
                return $"https://{host}/viewjob?jk={Uri.EscapeDataString(jk)}";

            // Broken sponsored link with no job key — do not keep it.
            if (uri.AbsolutePath.Contains("pagead/clk", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(uri.Query))
                return null;

            // Keep query when present; stripping it breaks Indeed deep links.
            var indeedBuilder = new UriBuilder(uri)
            {
                Scheme = uri.Scheme.ToLowerInvariant(),
                Host = host,
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                Fragment = string.Empty
            };
            var indeedPath = indeedBuilder.Path.TrimEnd('/');
            indeedBuilder.Path = string.IsNullOrEmpty(indeedPath) ? "/" : indeedPath;
            return indeedBuilder.Uri.ToString();
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = host,
            Port = uri.IsDefaultPort ? -1 : uri.Port,
            Query = string.Empty,
            Fragment = string.Empty
        };
        var path = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrEmpty(path) ? "/" : path;
        return builder.Uri.ToString();
    }

    private static bool IsIndeedHost(string host) =>
        host.Equals("indeed.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".indeed.com", StringComparison.OrdinalIgnoreCase);

    private static string? GetQueryParam(Uri uri, string key)
    {
        var query = uri.Query;
        if (string.IsNullOrEmpty(query))
            return null;

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 0)
                continue;
            if (!string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
                continue;
            return pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
        }

        return null;
    }

    private static string NormalizeKeyPart(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .Trim()
                .ToLowerInvariant();

    private static async Task<List<ScrapedOffer>> ExtractOffersAsync(
        IPage page, JobPortalDto portal, ScrapeConfig config, string pageUrl, string? countryName)
    {
        var offers = new List<ScrapedOffer>();
        IReadOnlyList<IElementHandle> cards = Array.Empty<IElementHandle>();

        foreach (var listSelector in config.ListSelectors)
        {
            cards = await page.QuerySelectorAllAsync(listSelector);
            if (cards.Count > 0)
                break;
        }

        if (cards.Count == 0)
            return offers;

        var resolvedCountry = !string.IsNullOrWhiteSpace(countryName)
            ? countryName
            : ResolveCountryFromUrl(pageUrl, config) ?? config.DefaultCountry;

        foreach (var card in cards.Take(config.MaxItems))
        {
            var title = await FirstTextAsync(card, config.TitleSelectors);
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var href = await FirstAttrAsync(card, config.LinkSelectors, "href");
            var jobKey = await FirstAttrAsync(card, config.LinkSelectors, "data-jk")
                ?? await FirstAttrAsync(card, ["[data-jk]"], "data-jk");
            var company = await FirstTextAsync(card, config.CompanySelectors);
            var location = await FirstTextAsync(card, config.LocationSelectors);
            var snippet = await FirstTextAsync(card, config.SnippetSelectors);
            var postedAt = await FirstTextAsync(card, config.DateSelectors);
            var modalityRaw = await FirstTextAsync(card, config.ModalitySelectors);
            var contractRaw = await FirstTextAsync(card, config.ContractSelectors);

            var postedAtText = Clean(postedAt);
            DateTime? postedAtUtc = OfferDateParser.TryParsePostedAtUtc(postedAtText);

            string? cardText = null;
            var needsCardText =
                string.IsNullOrWhiteSpace(company) ||
                string.IsNullOrWhiteSpace(location) ||
                postedAtUtc is null ||
                string.IsNullOrWhiteSpace(modalityRaw) ||
                string.IsNullOrWhiteSpace(contractRaw);

            if (needsCardText)
                cardText = (await card.InnerTextAsync()) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(location))
            {
                company ??= GuessByKeywords(cardText ?? string.Empty, config.CompanyKeywords);
                location ??= GuessByKeywords(cardText ?? string.Empty, config.LocationKeywords);
            }

            if (postedAtUtc is null)
            {
                var cardTextClean = Clean(cardText);
                postedAtText = cardTextClean;
                postedAtUtc = OfferDateParser.TryParsePostedAtUtc(cardTextClean);
            }

            var workModality = OfferFieldNormalizer.NormalizeModality(modalityRaw)
                ?? OfferFieldNormalizer.GuessModalityFromText(cardText);

            var contractType = OfferFieldNormalizer.NormalizeContractType(contractRaw)
                ?? OfferFieldNormalizer.GuessContractFromText(cardText);

            var absolute = AbsoluteUrl(pageUrl, href);

            // Indeed: prefer data-jk → stable /viewjob?jk=... (pagead/clk needs query params).
            if (!string.IsNullOrWhiteSpace(jobKey)
                && Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
                && IsIndeedHost(pageUri.Host))
            {
                absolute =
                    $"https://{pageUri.Host.ToLowerInvariant()}/viewjob?jk={Uri.EscapeDataString(jobKey.Trim())}";
            }

            absolute = NormalizeOfferUrl(absolute);
            var cleanTitle = Clean(title)!;
            var cleanCompany = Clean(company);
            offers.Add(new ScrapedOffer
            {
                Title = cleanTitle,
                Company = cleanCompany,
                Location = Clean(location),
                Url = absolute,
                DescriptionSnippet = Clean(snippet),
                PostedAtText = postedAtText,
                PostedAt = postedAtUtc,
                Country = resolvedCountry,
                Language = config.DefaultLanguage,
                WorkModality = workModality,
                ContractType = contractType,
                Source = portal.Name,
                ExternalKey = absolute ?? $"{NormalizeKeyPart(cleanTitle)}|{NormalizeKeyPart(cleanCompany)}"
            });
        }

        return offers;
    }

    private static string? ResolveCountryFromUrl(string pageUrl, ScrapeConfig config)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) || config.SearchCountries.Count == 0)
            return null;

        var host = uri.Host.ToLowerInvariant();
        foreach (var country in config.SearchCountries)
        {
            if (string.IsNullOrWhiteSpace(country.Code))
                continue;
            var code = country.Code.Trim().ToLowerInvariant();
            if (host.StartsWith($"{code}.", StringComparison.Ordinal)
                || host.Contains($".{code}.", StringComparison.Ordinal)
                || host.EndsWith($".{code}", StringComparison.Ordinal))
                return country.Name;
        }

        return null;
    }

    private async Task<(int Enriched, int AuthWallHits)> EnrichDescriptionsAsync(
        IPage listPage,
        List<ScrapedOffer> offers,
        ScrapeConfig config,
        CancellationToken cancellationToken)
    {
        var max = Math.Clamp(config.MaxDetailPages, 1, Math.Max(1, offers.Count));
        var toVisit = offers
            .Where(o => !string.IsNullOrWhiteSpace(o.Url) && !LooksLikeApplyUrl(o.Url!))
            .OrderBy(o => string.IsNullOrWhiteSpace(o.Description) ? 0 : 1)
            .Take(max)
            .ToList();

        if (toVisit.Count == 0)
            return (0, 0);

        var enriched = 0;
        var authWallHits = 0;
        var skipLinkedInDetailNavigation = false;
        IPage? detailPage = null;
        try
        {
            // LinkedIn: el panel del listado usa la sesión logueada.
            // Abrir /jobs/view en otra pestaña suele mostrar authwall (“Únete a LinkedIn”).
            var needsExtraTab = toVisit.Any(o => !IsLinkedInUrl(o.Url!));
            if (needsExtraTab)
                detailPage = await listPage.Context.NewPageAsync();

            foreach (var offer in toVisit)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    string? description = null;

                    if (IsComputrabajoUrl(offer.Url!))
                    {
                        detailPage ??= await listPage.Context.NewPageAsync();
                        description = await TryExtractComputrabajoFromListPanelAsync(listPage, offer);

                        if (IsWeakComputrabajoDescription(description))
                        {
                            await detailPage.GotoAsync(offer.Url!, new PageGotoOptions
                            {
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 45_000
                            });
                            await PrepareComputrabajoDetailAsync(detailPage);
                            if (!LooksLikeLoginWall(detailPage.Url)
                                && !LooksLikeComputrabajoCountryGate(detailPage.Url))
                            {
                                description = PreferRicherDescription(
                                    description,
                                    await ExtractComputrabajoDescriptionAsync(detailPage));
                                await TryEnrichComputrabajoMetaAsync(detailPage, offer);
                            }
                        }

                        if (IsWeakComputrabajoDescription(description))
                        {
                            var printUrl = BuildComputrabajoPrintUrl(offer.Url!);
                            if (!string.IsNullOrWhiteSpace(printUrl))
                            {
                                await detailPage.GotoAsync(printUrl, new PageGotoOptions
                                {
                                    WaitUntil = WaitUntilState.DOMContentLoaded,
                                    Timeout = 45_000
                                });
                                await detailPage.WaitForTimeoutAsync(Math.Max(800, _options.SlowMoMs));
                                description = PreferRicherDescription(
                                    description,
                                    await ExtractComputrabajoDescriptionAsync(detailPage));
                            }
                        }
                    }
                    else if (IsLinkedInUrl(offer.Url!))
                    {
                        if (LooksLikeLoginWall(listPage.Url)
                            || await LooksLikeLinkedInAuthWallPageAsync(listPage))
                        {
                            authWallHits++;
                            skipLinkedInDetailNavigation = true;
                            _logger.LogWarning(
                                "LinkedIn authwall en el listado. Inicia sesión en Chrome (debug :9222) y relanza. Oferta: {Url}",
                                offer.Url);
                            continue;
                        }

                        // Solo panel derecho del listado (misma sesión).
                        description = await TryExtractLinkedInFromListPanelAsync(listPage, offer);

                        // Último recurso /jobs/view — si sale authwall, no repetir en esta corrida.
                        if (IsWeakLinkedInDescription(description) && !skipLinkedInDetailNavigation)
                        {
                            detailPage ??= await listPage.Context.NewPageAsync();
                            var viewUrl = NormalizeLinkedInJobViewUrl(offer.Url!) ?? offer.Url!;
                            await detailPage.GotoAsync(viewUrl, new PageGotoOptions
                            {
                                WaitUntil = WaitUntilState.DOMContentLoaded,
                                Timeout = 45_000
                            });
                            await detailPage.WaitForTimeoutAsync(900);

                            if (LooksLikeLoginWall(detailPage.Url)
                                || await LooksLikeLinkedInAuthWallPageAsync(detailPage))
                            {
                                authWallHits++;
                                skipLinkedInDetailNavigation = true;
                                _logger.LogWarning(
                                    "LinkedIn authwall en /jobs/view ({Url}). Se deja de abrir detalles; solo panel del listado.",
                                    offer.Url);
                            }
                            else
                            {
                                var ready = await WaitForLinkedInAboutSectionAsync(detailPage, maxAttempts: 5);
                                if (!ready)
                                {
                                    _logger.LogWarning(
                                        "LinkedIn detalle sin 'Acerca del empleo' tras reintentos: {Url}",
                                        offer.Url);
                                }
                                else
                                {
                                    description = PreferRicherDescription(
                                        description,
                                        await ExtractLinkedInDescriptionAsync(detailPage));
                                }
                            }
                        }
                    }
                    else
                    {
                        detailPage ??= await listPage.Context.NewPageAsync();
                        await detailPage.GotoAsync(offer.Url!, new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded,
                            Timeout = 45_000
                        });
                        await detailPage.WaitForTimeoutAsync(Math.Max(500, _options.SlowMoMs));

                        if (LooksLikeLoginWall(detailPage.Url))
                        {
                            authWallHits++;
                            _logger.LogWarning("Detalle omitido (login): {Url}", offer.Url);
                            continue;
                        }

                        if (IsIndeedUrl(offer.Url!))
                        {
                            description = await ExtractIndeedDescriptionAsync(detailPage);
                        }
                        else
                        {
                            var selectors = config.DescriptionSelectors.Count > 0
                                ? config.DescriptionSelectors
                                : DefaultDescriptionSelectors(offer.Url!);
                            description = await FirstTextFromPageAsync(detailPage, selectors)
                                ?? await FirstTextFromPageAsync(detailPage,
                                [
                                    "main",
                                    "article",
                                    "[role='main']"
                                ]);
                        }
                    }

                    var cleaned = IsComputrabajoUrl(offer.Url!) || IsLinkedInUrl(offer.Url!)
                        ? CleanDescription(description, 50_000)
                        : CleanLong(description, 50_000);

                    if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 40)
                    {
                        _logger.LogWarning(
                            "Sin descripción útil en detalle ({Len} chars): {Url}",
                            cleaned?.Length ?? 0,
                            offer.Url);
                        continue;
                    }

                    if (IsComputrabajoUrl(offer.Url!))
                    {
                        cleaned = TrimComputrabajoNoise(cleaned);
                        if (IsWeakComputrabajoDescription(cleaned))
                        {
                            _logger.LogWarning(
                                "Descripción Computrabajo incompleta ({Len} chars), no se guarda: {Url}",
                                cleaned.Length,
                                offer.Url);
                            continue;
                        }
                    }

                    if (IsLinkedInUrl(offer.Url!) && IsWeakLinkedInDescription(cleaned))
                    {
                        _logger.LogWarning(
                            "Descripción LinkedIn incompleta ({Len} chars), no se guarda: {Url}",
                            cleaned.Length,
                            offer.Url);
                        continue;
                    }

                    offer.Description = cleaned;
                    offer.DescriptionSnippet = CleanLong(cleaned, 2000);

                    offer.WorkModality ??= OfferFieldNormalizer.GuessModalityFromText(cleaned);
                    offer.ContractType ??= OfferFieldNormalizer.GuessContractFromText(cleaned);

                    enriched++;
                }
                catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
                {
                    _logger.LogWarning(ex, "No se pudo abrir detalle {Url}", offer.Url);
                }

                if (_options.DelayBetweenUrlsMs > 0 && detailPage is not null)
                    await detailPage.WaitForTimeoutAsync(Math.Min(_options.DelayBetweenUrlsMs, 2500));
            }
        }
        finally
        {
            if (detailPage is not null)
            {
                try { await detailPage.CloseAsync(); }
                catch { /* ignore */ }
            }
        }

        _logger.LogInformation(
            "Descripciones enriquecidas: {Enriched}/{Tried} (authwall={AuthWall})",
            enriched,
            toVisit.Count,
            authWallHits);
        return (enriched, authWallHits);
    }

    private static bool IsComputrabajoUrl(string url) =>
        url.Contains("computrabajo.", StringComparison.OrdinalIgnoreCase);

    private static bool IsLinkedInUrl(string url) =>
        url.Contains("linkedin.", StringComparison.OrdinalIgnoreCase);

    private static bool IsIndeedUrl(string url) =>
        url.Contains("indeed.", StringComparison.OrdinalIgnoreCase);

    private async Task PaginateLinkedInSearchAsync(
        IPage page,
        JobPortalDto portal,
        ScrapeConfig config,
        ScrapeTarget target,
        IReadOnlyList<ScrapedOffer> firstPageOffers,
        List<ScrapedOffer> allOffers,
        CancellationToken cancellationToken)
    {
        var maxPages = Math.Clamp(config.MaxSearchPages, 1, 40);
        var pageSize = Math.Clamp(config.PaginationPageSize <= 1 ? 25 : config.PaginationPageSize, 10, 50);

        if (SearchPageExhaustedAgeWindow(firstPageOffers, config) || firstPageOffers.Count == 0)
            return;

        for (var pageIndex = 1; pageIndex < maxPages; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ScrollLinkedInListToPaginationAsync(page);

            var clicked = await ClickLinkedInNextPageAsync(page);
            if (!clicked)
            {
                // Fallback: start=25,50… (LinkedIn lista ~25 por página).
                var start = pageIndex * pageSize;
                var nextUrl = WithQueryParam(target.Url, "start", start.ToString());
                _logger.LogInformation(
                    "LinkedIn página {Page}/{Max} vía URL start={Start}",
                    pageIndex + 1,
                    maxPages,
                    start);

                await page.GotoAsync(nextUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 60_000
                });
                await page.WaitForTimeoutAsync(Math.Max(900, _options.SlowMoMs));
                await EnsureLinkedInCountryGeoAsync(page, target);
            }
            else
            {
                _logger.LogInformation(
                    "LinkedIn página {Page}/{Max} (botón Siguiente)",
                    pageIndex + 1,
                    maxPages);
                await page.WaitForTimeoutAsync(Math.Max(1000, _options.SlowMoMs));
            }

            if (!string.IsNullOrWhiteSpace(config.WaitForSelector))
            {
                try
                {
                    await page.WaitForSelectorAsync(config.WaitForSelector, new PageWaitForSelectorOptions
                    {
                        Timeout = 15_000
                    });
                }
                catch (TimeoutException)
                {
                    _logger.LogInformation("LinkedIn: sin más resultados en página {Page}", pageIndex + 1);
                    break;
                }
            }

            for (var i = 0; i < Math.Min(config.ScrollTimes, 5); i++)
            {
                try
                {
                    await page.Mouse.MoveAsync(420, 520);
                    await page.Mouse.WheelAsync(0, 1100);
                }
                catch
                {
                    /* ignore */
                }

                await page.WaitForTimeoutAsync(Math.Max(350, _options.SlowMoMs));
            }

            List<ScrapedOffer> batch;
            try
            {
                batch = await ExtractOffersAsync(page, portal, config, page.Url, target.CountryName);
            }
            catch (PlaywrightException ex) when (IsDestroyedContext(ex))
            {
                _logger.LogWarning(ex, "LinkedIn: contexto destruido en página {Page}", pageIndex + 1);
                break;
            }

            if (batch.Count == 0)
            {
                _logger.LogInformation("LinkedIn: página {Page} vacía, fin de paginación", pageIndex + 1);
                break;
            }

            // Evitar acumular la misma página si el botón no avanzó.
            var existingKeys = new HashSet<string>(
                allOffers
                    .Select(o => NormalizeOfferUrl(o.Url) ?? o.Title ?? "")
                    .Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);
            var fresh = batch
                .Where(o =>
                {
                    var key = NormalizeOfferUrl(o.Url) ?? o.Title ?? "";
                    return !string.IsNullOrWhiteSpace(key) && existingKeys.Add(key);
                })
                .ToList();

            if (fresh.Count == 0)
            {
                _logger.LogInformation(
                    "LinkedIn: página {Page} sin ofertas nuevas (posible fin o misma página)",
                    pageIndex + 1);
                break;
            }

            allOffers.AddRange(fresh);
            _logger.LogInformation(
                "LinkedIn: página {Page} +{New} ofertas (total acumulado {Total})",
                pageIndex + 1,
                fresh.Count,
                allOffers.Count);

            if (SearchPageExhaustedAgeWindow(fresh, config))
            {
                _logger.LogInformation(
                    "LinkedIn: página {Page} fuera de la ventana de {Hours}h; se detiene la paginación",
                    pageIndex + 1,
                    config.MaxAgeHours);
                break;
            }

            if (_options.DelayBetweenUrlsMs > 0)
                await page.WaitForTimeoutAsync(Math.Min(_options.DelayBetweenUrlsMs, 2500));
        }
    }

    private async Task ScrollLinkedInListToPaginationAsync(IPage page)
    {
        try
        {
            await page.EvaluateAsync(@"() => {
              const selectors = [
                '.jobs-search-results-list',
                '.scaffold-layout__list',
                '.scaffold-layout__list > div',
                'div.jobs-search-results-list',
                'ul.scaffold-layout__list-container'
              ];
              for (const sel of selectors) {
                const el = document.querySelector(sel);
                if (el && el.scrollHeight > el.clientHeight + 40) {
                  el.scrollTop = el.scrollHeight;
                  return true;
                }
              }
              window.scrollTo(0, document.body.scrollHeight);
              return false;
            }");
            await page.WaitForTimeoutAsync(500);
        }
        catch
        {
            /* ignore */
        }
    }

    private async Task<bool> ClickLinkedInNextPageAsync(IPage page)
    {
        try
        {
            var next = page.Locator(
                "button.jobs-search-pagination__button--next:not([disabled]), " +
                "button[aria-label='Ver siguiente página']:not([disabled]), " +
                "button[aria-label='Next']:not([disabled]), " +
                ".jobs-search-pagination button:has-text('Siguiente'):not([disabled])");

            if (await next.CountAsync() == 0)
                return false;

            var btn = next.First;
            if (!await btn.IsVisibleAsync() || !await btn.IsEnabledAsync())
                return false;

            // Asegurar que el paginador esté en vista.
            try { await btn.ScrollIntoViewIfNeededAsync(); }
            catch { /* ignore */ }

            await btn.ClickAsync(new LocatorClickOptions { Timeout = 8_000 });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LinkedIn: no se pudo clicar Siguiente");
            return false;
        }
    }

    private async Task PaginateIndeedSearchAsync(
        IPage page,
        JobPortalDto portal,
        ScrapeConfig config,
        ScrapeTarget target,
        IReadOnlyList<ScrapedOffer> firstPageOffers,
        List<ScrapedOffer> allOffers,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(config.PaginationPageSize, 5, 50);
        // Tope de seguridad: el corte real es sin nuevas / fuera de 24h / sin resultados.
        var maxPages = Math.Clamp(config.MaxSearchPages <= 0 ? 30 : Math.Max(config.MaxSearchPages, 20), 1, 40);

        if (firstPageOffers.Count == 0)
            return;

        if (firstPageOffers.Count < Math.Min(5, pageSize))
        {
            _logger.LogInformation(
                "Indeed: {Count} resultados en página 1; no se pagina más",
                firstPageOffers.Count);
            return;
        }

        if (SearchPageExhaustedAgeWindow(firstPageOffers, config))
            return;

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in allOffers)
        {
            var key = IndeedOfferKey(o);
            if (!string.IsNullOrWhiteSpace(key))
                seenKeys.Add(key!);
        }

        for (var pageIndex = 1; pageIndex < maxPages; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var start = pageIndex * pageSize;
            var nextUrl = WithQueryParam(target.Url, "start", start.ToString());

            _logger.LogInformation(
                "Indeed siguiente página (start={Start}): {Url}",
                start,
                nextUrl);

            await page.GotoAsync(nextUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await page.WaitForTimeoutAsync(Math.Max(600, _options.SlowMoMs));

            if (await LooksLikeIndeedNoResultsAsync(page))
            {
                _logger.LogInformation("Indeed: sin más resultados (start={Start})", start);
                break;
            }

            if (!string.IsNullOrWhiteSpace(config.WaitForSelector))
            {
                try
                {
                    await page.WaitForSelectorAsync(config.WaitForSelector, new PageWaitForSelectorOptions
                    {
                        Timeout = 10_000
                    });
                }
                catch (TimeoutException)
                {
                    _logger.LogInformation("Indeed: listado vacío (start={Start})", start);
                    break;
                }
            }

            for (var i = 0; i < Math.Min(config.ScrollTimes, 2); i++)
            {
                await page.Mouse.WheelAsync(0, 800);
                await page.WaitForTimeoutAsync(Math.Max(250, _options.SlowMoMs));
            }

            List<ScrapedOffer> batch;
            try
            {
                batch = await ExtractOffersAsync(page, portal, config, page.Url, target.CountryName);
            }
            catch (PlaywrightException ex) when (IsDestroyedContext(ex))
            {
                _logger.LogWarning(ex, "Indeed: contexto destruido en start={Start}", start);
                break;
            }

            if (batch.Count == 0)
            {
                _logger.LogInformation("Indeed: 0 tarjetas en start={Start}; fin", start);
                break;
            }

            var fresh = batch
                .Where(o =>
                {
                    var key = IndeedOfferKey(o);
                    return !string.IsNullOrWhiteSpace(key) && seenKeys.Add(key!);
                })
                .ToList();

            if (fresh.Count == 0)
            {
                _logger.LogInformation(
                    "Indeed: start={Start} repite ofertas ya vistas; fin de paginación",
                    start);
                break;
            }

            allOffers.AddRange(fresh);
            _logger.LogInformation(
                "Indeed: +{New} nuevas en start={Start} (acumulado {Total})",
                fresh.Count,
                start,
                allOffers.Count);

            if (SearchPageExhaustedAgeWindow(fresh, config))
            {
                _logger.LogInformation(
                    "Indeed: start={Start} fuera de {Hours}h; fin de paginación",
                    start,
                    config.MaxAgeHours);
                break;
            }

            if (fresh.Count < 3)
            {
                _logger.LogInformation(
                    "Indeed: pocas nuevas ({New}) en start={Start}; fin de paginación",
                    fresh.Count,
                    start);
                break;
            }

            if (_options.DelayBetweenUrlsMs > 0)
                await page.WaitForTimeoutAsync(Math.Min(_options.DelayBetweenUrlsMs, 1500));
        }
    }

    /// <summary>
    /// Indeed: solo candidatas con .NET/React/… en título o snippet, antes de abrir detalle.
    /// </summary>
    private static List<ScrapedOffer> PreFilterIndeedCandidates(
        IReadOnlyList<ScrapedOffer> offers,
        ScrapeConfig config)
    {
        var kept = new List<ScrapedOffer>(offers.Count);
        foreach (var offer in offers)
        {
            if (!string.IsNullOrWhiteSpace(offer.Url) && !IsIndeedUrl(offer.Url!))
            {
                kept.Add(offer);
                continue;
            }

            var require = ResolveRequireContentKeywords(config, offer.Country);
            if (require.Count == 0
                || OfferFieldNormalizer.MatchesRequiredTech(
                    offer.Title,
                    offer.DescriptionSnippet,
                    require))
            {
                kept.Add(offer);
            }
        }

        return kept;
    }

    private static string? IndeedOfferKey(ScrapedOffer offer)
    {
        var url = NormalizeOfferUrl(offer.Url);
        if (!string.IsNullOrWhiteSpace(url))
            return url;
        if (!string.IsNullOrWhiteSpace(offer.Title) && !string.IsNullOrWhiteSpace(offer.Company))
            return $"{offer.Title}|{offer.Company}";
        return offer.Title;
    }

    private static async Task<bool> LooksLikeIndeedNoResultsAsync(IPage page)
    {
        try
        {
            var text = await page.InnerTextAsync("body");
            if (string.IsNullOrWhiteSpace(text))
                return false;
            var t = text.ToLowerInvariant();
            return t.Contains("no se han encontrado", StringComparison.Ordinal)
                   || t.Contains("did not match any jobs", StringComparison.Ordinal)
                   || t.Contains("no jobs found", StringComparison.Ordinal)
                   || t.Contains("0 empleos", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Con sort=date: si ninguna oferta parseable está dentro de maxAgeHours, agotamos la ventana.
    /// </summary>
    private static bool SearchPageExhaustedAgeWindow(
        IReadOnlyList<ScrapedOffer> batch,
        ScrapeConfig config)
    {
        if (config.MaxAgeHours is not int maxHours || batch.Count == 0)
            return false;

        var now = DateTime.UtcNow;
        var within = 0;
        var older = 0;

        foreach (var offer in batch)
        {
            var ageHours = OfferDateParser.TryParseAgeHours(offer.PostedAtText);
            if (ageHours is null && offer.PostedAt is DateTime postedAt)
                ageHours = Math.Max(0, (now - postedAt.ToUniversalTime()).TotalHours);

            if (ageHours is null)
                continue;

            if (ageHours.Value <= maxHours)
                within++;
            else
                older++;
        }

        return within == 0 && older > 0;
    }

    private static string WithQueryParam(string url, string name, string value)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var parts = new List<string>();
        var replaced = false;
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = pair.IndexOf('=');
                var key = eq >= 0 ? pair[..eq] : pair;
                if (string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
                    replaced = true;
                }
                else
                {
                    parts.Add(pair);
                }
            }
        }

        if (!replaced)
            parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");

        var builder = new UriBuilder(uri) { Query = string.Join('&', parts) };
        return builder.Uri.ToString();
    }

    /// <summary>
    /// Indeed: prioriza el bloque bajo "Acerca del empleo" / "About the job".
    /// </summary>
    private static async Task<string?> ExtractIndeedDescriptionAsync(IPage page)
    {
        try
        {
            var fromHeading = await page.EvaluateAsync<string?>(@"() => {
              const normalize = (s) => (s || '').replace(/\s+/g, ' ').trim();
              const isStart = (t) => /acerca del empleo|about the job|descripci[oó]n del puesto|descripci[oó]n del empleo|job description/i.test(t || '');
              const isStop = (t) => /acerca de la empresa|about the company|beneficios|benefits|salario|salary|evaluaciones|reviews|preguntas frecuentes|similar jobs|empleos similares|reportar|report job/i.test(t || '');

              const roots = [
                document.querySelector('#jobDescriptionText'),
                document.querySelector('#job-description'),
                document.querySelector('[data-testid=""jobsearch-JobComponent-description""]'),
                document.querySelector('.jobsearch-JobComponent-description'),
                document.querySelector('[id*=""jobDescription""]'),
                document.querySelector('main'),
                document.body
              ].filter(Boolean);

              for (const root of roots) {
                const headings = Array.from(root.querySelectorAll('h1,h2,h3,h4,[role=""heading""],div[class*=""heading""]'));
                const startEl = headings.find(h => isStart(normalize(h.textContent)));
                if (startEl) {
                  const parts = [];
                  let node = startEl.nextElementSibling;
                  while (node) {
                    const text = normalize(node.innerText || node.textContent);
                    if (text && isStop(text) && text.length < 80) break;
                    const heading = node.matches?.('h1,h2,h3,h4,[role=""heading""]') ? normalize(node.textContent) : '';
                    if (heading && isStop(heading)) break;
                    if (text) parts.push(node.innerText || node.textContent || '');
                    node = node.nextElementSibling;
                  }
                  // Si el heading está dentro del mismo contenedor de descripción, tomar el contenedor completo.
                  if (parts.join('\n').trim().length < 80) {
                    const container = startEl.closest('#jobDescriptionText, #job-description, [id*=""jobDescription""], .jobsearch-JobComponent-description')
                      || startEl.parentElement;
                    if (container) {
                      const full = (container.innerText || '').trim();
                      if (full.length >= 40) return full;
                    }
                  } else {
                    return parts.join('\n').trim();
                  }
                }
              }

              const classic = document.querySelector('#jobDescriptionText')
                || document.querySelector('#job-description')
                || document.querySelector('[data-testid=""jobsearch-JobComponent-description""]')
                || document.querySelector('.jobsearch-JobComponent-description');
              return classic ? (classic.innerText || '').trim() : null;
            }");

            if (!string.IsNullOrWhiteSpace(fromHeading) && fromHeading.Trim().Length >= 40)
                return fromHeading.Trim();
        }
        catch
        {
            /* fallback selectors */
        }

        return await FirstTextFromPageAsync(page,
        [
            "#jobDescriptionText",
            "#job-description",
            "[data-testid='jobsearch-JobComponent-description']",
            ".jobsearch-JobComponent-description",
            "[id*='jobDescription']"
        ]);
    }

    private async Task EnsureLinkedInCountryGeoAsync(IPage page, ScrapeTarget target)
    {
        if (!IsLinkedInUrl(target.Url))
            return;

        if (!Uri.TryCreate(target.Url, UriKind.Absolute, out var expectedUri))
            return;

        var expectedGeo = GetQueryParam(expectedUri, "geoId");
        if (string.IsNullOrWhiteSpace(expectedGeo))
            return;

        await page.WaitForTimeoutAsync(700);

        string? currentGeo = null;
        if (Uri.TryCreate(page.Url, UriKind.Absolute, out var currentUri))
            currentGeo = GetQueryParam(currentUri, "geoId");

        if (string.Equals(currentGeo, expectedGeo, StringComparison.OrdinalIgnoreCase))
            return;

        _logger.LogWarning(
            "LinkedIn cambió geoId de {Expected} a {Current} (suele ser la ciudad del perfil). Reaplicando país={Country}.",
            expectedGeo,
            currentGeo ?? "(vacío)",
            target.CountryName ?? "?");

        // Forzar de nuevo la URL con geoId de país + origin para que respete el filtro.
        var forced = target.Url;
        if (!forced.Contains("origin=", StringComparison.OrdinalIgnoreCase))
            forced += (forced.Contains('?', StringComparison.Ordinal) ? "&" : "?")
                      + "origin=JOB_SEARCH_PAGE_JOB_FILTER";

        await page.GotoAsync(forced, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.WaitForTimeoutAsync(1200);

        if (Uri.TryCreate(page.Url, UriKind.Absolute, out var afterUri))
        {
            var afterGeo = GetQueryParam(afterUri, "geoId");
            if (!string.Equals(afterGeo, expectedGeo, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "LinkedIn sigue sin respetar geoId={Expected} (actual={Current}). Revisa el filtro de ubicación en la UI.",
                    expectedGeo,
                    afterGeo ?? "(vacío)");
            }
        }
    }

    private async Task PrepareLinkedInDetailAsync(IPage page)
    {
        // No saltar por un sleep fijo: reintentar hasta ver "Acerca del empleo" / texto expandible.
        await WaitForLinkedInAboutSectionAsync(page, maxAttempts: 5);
    }

    /// <summary>
    /// Espera y prepara el panel de detalle LinkedIn hasta hallar "Acerca del empleo"
    /// (o expandable-text-box). No avanza a otra oferta/página por tiempo solo.
    /// </summary>
    private async Task<bool> WaitForLinkedInAboutSectionAsync(IPage page, int maxAttempts = 5)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await ScrollLinkedInDetailsIntoViewAsync(page);
            await ExpandLinkedInDescriptionAsync(page);

            var markers = page.Locator(
                "[data-testid='expandable-text-box'], " +
                "h2:has-text('Acerca del empleo'), h2:has-text('About the job'), " +
                ".jobs-description__content, .jobs-box__html-content, #job-details, " +
                "article.jobs-description, .jobs-description-content__text");

            try
            {
                await markers.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 4_000
                });
            }
            catch (TimeoutException)
            {
                _logger.LogDebug(
                    "LinkedIn: intento {Attempt}/{Max} sin marcador de descripción aún ({Url})",
                    attempt,
                    maxAttempts,
                    page.Url);
                await page.WaitForTimeoutAsync(700);
                continue;
            }

            // Preferir texto real en expandable-text-box (UI nueva).
            var box = page.Locator("[data-testid='expandable-text-box']").First;
            if (await box.CountAsync() > 0 && await box.IsVisibleAsync())
            {
                // Aún puede estar colapsado: expandir otra vez y medir.
                await ExpandLinkedInDescriptionAsync(page);
                await page.WaitForTimeoutAsync(350);
                try
                {
                    var text = (await box.InnerTextAsync())?.Trim() ?? "";
                    if (text.Length >= 60)
                        return true;
                }
                catch
                {
                    /* reintentar */
                }
            }

            // Heading visible = sección cargada; la extracción leerá el contenedor.
            var aboutH = page.Locator("h2:has-text('Acerca del empleo'), h2:has-text('About the job')");
            if (await aboutH.CountAsync() > 0 && await aboutH.First.IsVisibleAsync())
                return true;

            // Legacy body
            var legacy = page.Locator(".jobs-description__content, .jobs-box__html-content, #job-details");
            if (await legacy.CountAsync() > 0)
            {
                try
                {
                    var t = (await legacy.First.InnerTextAsync())?.Trim() ?? "";
                    if (t.Length >= 60)
                        return true;
                }
                catch
                {
                    /* reintentar */
                }
            }

            await page.WaitForTimeoutAsync(600);
        }

        _logger.LogWarning(
            "LinkedIn: no apareció 'Acerca del empleo' / descripción tras {Attempts} intentos en {Url}",
            maxAttempts,
            page.Url);
        return false;
    }

    private async Task ExpandLinkedInDescriptionAsync(IPage page)
    {
        try
        {
            var expandBtn = page.Locator(
                "[data-testid='expandable-text-button'] span, " +
                "button[data-testid='expandable-text-button'], " +
                "button.jobs-description__footer-button, " +
                "button:has-text('See more'), button:has-text('Ver más'), " +
                "button:has-text('Show more'), button:has-text('… más'), button:has-text('... más')");
            var count = await expandBtn.CountAsync();
            for (var i = 0; i < Math.Min(count, 4); i++)
            {
                var btn = expandBtn.Nth(i);
                if (!await btn.IsVisibleAsync())
                    continue;
                try
                {
                    await btn.ClickAsync(new LocatorClickOptions { Timeout = 2_000, Force = true });
                    await page.WaitForTimeoutAsync(300);
                }
                catch
                {
                    /* optional */
                }
            }
        }
        catch
        {
            /* optional */
        }
    }

    private async Task ScrollLinkedInDetailsIntoViewAsync(IPage page)
    {
        try
        {
            await page.EvaluateAsync(@"() => {
              const panes = [
                '.jobs-search__job-details',
                '.jobs-details',
                '.scaffold-layout__detail',
                '.job-view-layout',
                'main'
              ];
              for (const sel of panes) {
                const el = document.querySelector(sel);
                if (el) {
                  el.scrollTop = Math.min(el.scrollTop + 420, el.scrollHeight);
                  const about = [...el.querySelectorAll('h2,h3')].find(h =>
                    /acerca del empleo|about the job/i.test(h.textContent || ''));
                  if (about) about.scrollIntoView({ block: 'center' });
                  return;
                }
              }
              const about = [...document.querySelectorAll('h2,h3')].find(h =>
                /acerca del empleo|about the job/i.test(h.textContent || ''));
              if (about) about.scrollIntoView({ block: 'center' });
            }");
        }
        catch
        {
            /* ignore — puede fallar por CSP; locators siguen */
        }
    }

    private async Task<string?> TryExtractLinkedInFromListPanelAsync(IPage listPage, ScrapedOffer offer)
    {
        if (!IsLinkedInUrl(listPage.Url))
            return null;

        if (LooksLikeLoginWall(listPage.Url) || await LooksLikeLinkedInAuthWallPageAsync(listPage))
            return null;

        var jobId = ExtractLinkedInJobId(offer.Url!);
        try
        {
            ILocator card;
            if (!string.IsNullOrWhiteSpace(jobId))
            {
                card = listPage.Locator(
                    $"div[data-job-id='{jobId}'], li[data-occludable-job-id='{jobId}'], a[href*='/jobs/view/{jobId}'], a[href*='currentJobId={jobId}']");
            }
            else
            {
                var title = (offer.Title ?? "").Trim();
                if (title.Length < 3)
                    return null;
                card = listPage.Locator("li.jobs-search-results__list-item, div.job-card-container, div.base-card")
                    .Filter(new LocatorFilterOptions { HasTextString = title.Length > 60 ? title[..60] : title });
            }

            if (await card.CountAsync() == 0)
                return null;

            await card.First.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
            // Esperar el panel derecho — no un sleep y saltar.
            var ready = await WaitForLinkedInAboutSectionAsync(listPage, maxAttempts: 5);
            if (!ready)
                return null;

            if (LooksLikeLoginWall(listPage.Url) || await LooksLikeLinkedInAuthWallPageAsync(listPage))
                return null;

            return await ExtractLinkedInDescriptionAsync(listPage);
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            _logger.LogDebug(ex, "No se pudo enriquecer LinkedIn desde panel jobId={JobId}", jobId);
            return null;
        }
    }

    private static async Task<string?> ExtractLinkedInDescriptionAsync(IPage page)
    {
        // 1) Locators primero (más fiable con CSP que EvaluateAsync).
        try
        {
            var box = page.Locator("[data-testid='expandable-text-box']").First;
            if (await box.CountAsync() > 0 && await box.IsVisibleAsync())
            {
                var text = (await box.InnerTextAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length >= 60)
                {
                    var title = await SafeInnerTextAsync(page,
                        ".job-details-jobs-unified-top-card__job-title, h1.t-24, h1");
                    var company = await SafeInnerTextAsync(page,
                        ".job-details-jobs-unified-top-card__company-name, .job-details-jobs-unified-top-card__primary-description-container a");
                    return BuildLinkedInDescription(title, company, null, text);
                }
            }

            var aboutH = page.Locator("h2:has-text('Acerca del empleo'), h2:has-text('About the job')").First;
            if (await aboutH.CountAsync() > 0)
            {
                // Contenedor ancestro que incluye heading + cuerpo.
                var section = aboutH.Locator(
                    "xpath=ancestor::div[.//*[@data-testid='expandable-text-box'] or .//p][1]");
                if (await section.CountAsync() > 0)
                {
                    var text = (await section.First.InnerTextAsync())?.Trim();
                    if (!string.IsNullOrWhiteSpace(text) && text.Length >= 80)
                    {
                        text = System.Text.RegularExpressions.Regex.Replace(
                            text, @"^(Acerca del empleo|About the job)\s*", "", 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                        var title = await SafeInnerTextAsync(page,
                            ".job-details-jobs-unified-top-card__job-title, h1.t-24, h1");
                        var company = await SafeInnerTextAsync(page,
                            ".job-details-jobs-unified-top-card__company-name, .job-details-jobs-unified-top-card__primary-description-container a");
                        return BuildLinkedInDescription(title, company, null, text);
                    }
                }
            }

            var legacy = await FirstTextFromPageAsync(page,
            [
                ".jobs-description__content",
                ".jobs-box__html-content",
                "#job-details",
                "article.jobs-description",
                ".jobs-description-content__text",
                ".job-details-about-the-job-module__description",
                ".jobs-description"
            ]);
            if (!string.IsNullOrWhiteSpace(legacy) && legacy.Length >= 60)
            {
                var title = await SafeInnerTextAsync(page,
                    ".job-details-jobs-unified-top-card__job-title, h1.t-24, h1");
                var company = await SafeInnerTextAsync(page,
                    ".job-details-jobs-unified-top-card__company-name, .job-details-jobs-unified-top-card__primary-description-container a");
                return BuildLinkedInDescription(title, company, null, legacy);
            }
        }
        catch (PlaywrightException)
        {
            /* fallback eval */
        }

        // 2) Evaluate solo como respaldo.
        try
        {
            var about = await page.EvaluateAsync<string?>(
                """
                () => {
                  const norm = (s) => (s || '').replace(/\u00a0/g, ' ').replace(/[ \t]+\n/g, '\n').replace(/\n{3,}/g, '\n\n').trim();
                  const textOf = (el) => el ? norm(el.innerText || el.textContent || '') : '';
                  const headings = Array.from(document.querySelectorAll('h2, h3, [role="heading"]'));
                  const aboutH = headings.find(h => /acerca del empleo|about the job/i.test((h.textContent || '').trim()));
                  if (aboutH) {
                    let root = aboutH.closest('section, article, div') || aboutH.parentElement;
                    if (root && root.parentElement) {
                      const parentText = textOf(root.parentElement);
                      if (parentText.length > textOf(root).length + 40)
                        root = root.parentElement;
                    }
                    const box = (root && root.querySelector('[data-testid="expandable-text-box"]'))
                      || document.querySelector('[data-testid="expandable-text-box"]');
                    let body = textOf(box);
                    if (body.length < 60) body = textOf(root);
                    body = body.replace(/^Acerca del empleo\s*/i, '').replace(/^About the job\s*/i, '').trim();
                    if (body.length >= 60) return body;
                  }
                  const expand = document.querySelector('[data-testid="expandable-text-box"]');
                  const expandText = textOf(expand);
                  if (expandText.length >= 60) return expandText;
                  return null;
                }
                """);
            if (!string.IsNullOrWhiteSpace(about) && about.Length >= 60)
            {
                var title = await SafeInnerTextAsync(page,
                    ".job-details-jobs-unified-top-card__job-title, h1.t-24, h1");
                var company = await SafeInnerTextAsync(page,
                    ".job-details-jobs-unified-top-card__company-name, .job-details-jobs-unified-top-card__primary-description-container a");
                return BuildLinkedInDescription(title, company, null, about);
            }
        }
        catch (PlaywrightException)
        {
            /* ignore CSP */
        }

        return null;
    }

    private static string BuildLinkedInDescription(
        string? title, string? company, string? location, string body)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
            parts.Add("Título: " + title.Trim());
        if (!string.IsNullOrWhiteSpace(company))
            parts.Add("Empresa: " + company.Trim());
        if (!string.IsNullOrWhiteSpace(location))
            parts.Add("Ubicación: " + location.Trim());
        parts.Add("Descripción\n" + body.Trim());
        return string.Join("\n\n", parts);
    }

    private static async Task<string?> SafeInnerTextAsync(IPage page, string selector)
    {
        try
        {
            var loc = page.Locator(selector).First;
            if (await loc.CountAsync() == 0)
                return null;
            var t = (await loc.InnerTextAsync())?.Trim();
            return string.IsNullOrWhiteSpace(t) ? null : t;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsWeakLinkedInDescription(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;
        var t = text.Trim();
        if (t.Length < 120)
            return true;
        // Solo metadatos sin cuerpo.
        var lower = t.ToLowerInvariant();
        if (lower.Contains("descripción", StringComparison.Ordinal))
        {
            var idx = lower.IndexOf("descripción", StringComparison.Ordinal);
            var body = t[(idx + "descripción".Length)..].Trim();
            if (body.Length < 80)
                return true;
        }
        return false;
    }

    private static string? ExtractLinkedInJobId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var current = GetQueryParam(uri, "currentJobId");
        if (!string.IsNullOrWhiteSpace(current))
            return current;

        // /jobs/view/1234567890/
        var m = System.Text.RegularExpressions.Regex.Match(
            uri.AbsolutePath,
            @"/jobs/view/(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? NormalizeLinkedInJobViewUrl(string url)
    {
        var id = ExtractLinkedInJobId(url);
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return $"https://www.linkedin.com/jobs/view/{id}/";
    }

    private async Task PrepareComputrabajoDetailAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(Math.Max(900, _options.SlowMoMs));
        try
        {
            await page.WaitForSelectorAsync(
                "[div-link=\"oferta\"], .description_offer, [description-offer], .fs16.t_word_wrap, [data-offers-grid-detail-container], h3:has-text('Descripción de la oferta')",
                new PageWaitForSelectorOptions { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout esperando detalle Computrabajo en {Url}", page.Url);
        }

        // Pestaña / ancla "Oferta" (layout página completa).
        try
        {
            var ofertaTab = page.Locator("[div-link='oferta'], a[href*='#oferta'], [data-link='oferta']").First;
            if (await ofertaTab.CountAsync() > 0)
                await ofertaTab.ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
        }
        catch
        {
            /* optional */
        }

        // Expandir si la oferta aparece colapsada.
        try
        {
            var showMore = page.Locator("[show-desc-offer]:visible, span:has-text('Ver oferta completa'):visible").First;
            if (await showMore.CountAsync() > 0)
                await showMore.ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
        }
        catch
        {
            /* optional */
        }

        // Esperar cuerpo real (página completa o panel split-grid).
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const offerBlock = document.querySelector('[div-link="oferta"]');
                  if (offerBlock) {
                    const ps = [...offerBlock.querySelectorAll('p.mbB, p')]
                      .map(p => (p.innerText || '').trim())
                      .filter(t => t.length > 120 && !/^Palabras clave/i.test(t) && !/^Hace\s/i.test(t));
                    if (ps.length) return true;
                  }
                  const wrap = document.querySelector('.fs16.t_word_wrap, .description_offer .t_word_wrap');
                  if (wrap && (wrap.innerText || '').trim().length > 80) return true;
                  const root = document.querySelector('.description_offer, [description-offer]');
                  return !!root && (root.innerText || '').trim().length > 250;
                }
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 12_000 });
        }
        catch (TimeoutException)
        {
            /* seguimos con lo que haya */
        }

        await page.WaitForTimeoutAsync(400);
    }

    private async Task<string?> TryExtractComputrabajoFromListPanelAsync(IPage listPage, ScrapedOffer offer)
    {
        if (!IsComputrabajoUrl(listPage.Url))
            return null;

        var oi = ExtractComputrabajoOfferId(offer.Url!);
        if (string.IsNullOrWhiteSpace(oi))
            return null;

        try
        {
            // Click en la card de esta oferta para cargar el panel derecho completo.
            var cardLink = listPage.Locator(
                $"article.box_offer a[href*='{oi}'], div.box_offer a[href*='{oi}'], a.js-o-link[href*='{oi}']");
            if (await cardLink.CountAsync() == 0)
                return null;

            await cardLink.First.ClickAsync(new LocatorClickOptions { Timeout = 5_000 });
            await PrepareComputrabajoDetailAsync(listPage);
            var text = await ExtractComputrabajoDescriptionAsync(listPage);
            if (!IsWeakComputrabajoDescription(text))
                await TryEnrichComputrabajoMetaAsync(listPage, offer);
            return text;
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            _logger.LogDebug(ex, "No se pudo enriquecer desde panel listado oi={Oi}", oi);
            return null;
        }
    }

    /// <summary>
    /// Arma Description completa y organizada (layout página completa + panel split-grid).
    /// </summary>
    private static async Task<string?> ExtractComputrabajoDescriptionAsync(IPage page)
    {
        try
        {
            var organized = await page.EvaluateAsync<string?>(
                """
                () => {
                  const norm = (s) => (s || '').replace(/\u00a0/g, ' ').replace(/[ \t]+\n/g, '\n').replace(/\n{3,}/g, '\n\n').trim();
                  const textOf = (el) => el ? norm(el.innerText || el.textContent || '') : '';
                  const isJunkBody = (t) => {
                    const s = (t || '').replace(/\s+/g, ' ').trim();
                    if (s.length < 80) return true;
                    if (/^(oferta|empresa)(\s*\/?\s*(oferta|empresa))*$/i.test(s)) return true;
                    if (/^descripci[oó]n de la oferta$/i.test(s)) return true;
                    return false;
                  };
                  const pickLongest = (arr) => (arr || []).filter(Boolean).sort((a, b) => b.length - a.length)[0] || '';

                  const title = textOf(
                    document.querySelector('[data-offers-grid-detail-title], .title_offer, h1.title_offer, h1'));
                  const company = textOf(
                    document.querySelector('.header_detail a.dIB.mr10, .header_detail a[href*="/empleos"], .header_detail a, .box_offer a.fc_base'));
                  const location = textOf(
                    document.querySelector('.header_detail p.fs16.mb5'));

                  let meta = '';
                  let body = '';
                  let requirements = '';
                  let keywords = '';
                  let about = '';
                  let benefits = '';

                  // Layout A: página completa — [div-link="oferta"] (el HTML real de la vacante).
                  const offerBlock =
                    document.querySelector('[div-link="oferta"]') ||
                    [...document.querySelectorAll('div.mb40.pb40, div.mb40')]
                      .find(d => /Descripci[oó]n de la oferta/i.test(d.innerText || ''));

                  if (offerBlock) {
                    const tags = [...offerBlock.querySelectorAll('span.tag, .tag.base')]
                      .map(t => textOf(t))
                      .filter(t => t && t.length < 80 && !/aplic/i.test(t));
                    if (tags.length) meta = tags.join('\n');

                    const paras = [...offerBlock.querySelectorAll('p.mbB, p')]
                      .map(p => textOf(p))
                      .filter(t =>
                        t.length > 80 &&
                        !/^Palabras clave/i.test(t) &&
                        !/^Hace\s/i.test(t) &&
                        !/^Requerimientos$/i.test(t) &&
                        !isJunkBody(t));
                    body = pickLongest(paras);

                    // Si el párrafo útil no está marcado p.mbB, tomar texto del bloque menos ruido.
                    if (!body) {
                      const clone = offerBlock.cloneNode(true);
                      clone.querySelectorAll('script,style,svg,button,a.b_primary,.b_heart,.posSticky_m,noscript').forEach(e => e.remove());
                      let raw = textOf(clone);
                      raw = raw
                        .replace(/^Descripci[oó]n de la oferta\s*/i, '')
                        .replace(/\nAplicar[\s\S]*$/i, '')
                        .replace(/\nPostularme[\s\S]*$/i, '')
                        .replace(/\nAvísame con ofertas similares[\s\S]*$/i, '')
                        .replace(/\nDenunciar empleo[\s\S]*$/i, '')
                        .trim();
                      if (!isJunkBody(raw)) body = raw;
                    }

                    const reqTitle = [...offerBlock.querySelectorAll('p.fwB.fs18, p.fwB')]
                      .find(p => /requerimiento/i.test(p.innerText || ''));
                    const reqList = offerBlock.querySelector('ul.disc, ul.fs16.disc');
                    if (reqList)
                      requirements = (reqTitle ? textOf(reqTitle) : 'Requerimientos') + '\n' + textOf(reqList);

                    const kw = [...offerBlock.querySelectorAll('p.fc_aux')]
                      .map(p => textOf(p))
                      .find(t => /^Palabras clave/i.test(t));
                    if (kw) keywords = kw;
                  }

                  // Layout B: panel split-grid (.description_offer / t_word_wrap).
                  if (!body || isJunkBody(body)) {
                    if (!meta) {
                      meta = textOf(document.querySelector(
                        '.description_offer .fs14.mb10, [description-offer] .fs14.mb10'));
                    }
                    body = textOf(document.querySelector(
                      '.description_offer .fs16.t_word_wrap, [description-offer] .fs16.t_word_wrap, .fs16.t_word_wrap, #p_description'));

                    if (!body || isJunkBody(body)) {
                      const descRoot = document.querySelector('.description_offer, [description-offer]');
                      if (descRoot) {
                        const clone = descRoot.cloneNode(true);
                        clone.querySelectorAll('script,style,svg,button,noscript,.box_buttons,.popup,.opt_bubble,.graphic_bar,.doughnut,.stars,[data-complaint-overlay],#complaint-popup-container,[data-offers-grid-loading-container],.b_heart,.logo_company').forEach(e => e.remove());
                        let raw = textOf(clone);
                        for (const m of ['\nAcerca de', '\nSalarios', '\nEvaluaciones', 'profesionales recomiendan', '\nMostrar las ']) {
                          const i = raw.toLowerCase().indexOf(m.toLowerCase());
                          if (i > 80) raw = raw.slice(0, i).trim();
                        }
                        raw = raw.replace(/\nPostularme[\s\S]*$/i, '').trim();
                        if (!isJunkBody(raw) && raw.length > (body || '').length) body = raw;
                      }
                    }

                    if (!requirements) {
                      const reqTitle = [...document.querySelectorAll('.description_offer p.fwB.fs18, [description-offer] p.fwB.fs18, p.fwB.fs18')]
                        .find(p => /requerimiento/i.test(p.innerText || ''));
                      const reqList = document.querySelector('.description_offer ul.fs16.disc, [description-offer] ul.fs16.disc, ul.fs16.disc.mbB, ul.disc.mbB');
                      if (reqList)
                        requirements = (reqTitle ? textOf(reqTitle) : 'Requerimientos') + '\n' + textOf(reqList);
                    }

                    for (const sec of document.querySelectorAll('.description_offer .ptB.bt1.mtB, [description-offer] .ptB.bt1.mtB, .ptB.bt1.mtB')) {
                      const secText = textOf(sec);
                      if (/Acerca de/i.test(secText) && !about) {
                        const aboutTitle = textOf(sec.querySelector('p.fs18.mb20.fwB, p.fs18.fwB, p.fwB.fs18'));
                        const aboutBody = textOf(sec.querySelector('p[show-more]')) || '';
                        about = [aboutTitle || 'Acerca de la empresa', aboutBody].filter(Boolean).join('\n');
                        const benefitBlocks = [...sec.querySelectorAll('.w100.pl15')].map(x => textOf(x)).filter(Boolean);
                        if (benefitBlocks.length)
                          benefits = 'Beneficios\n' + benefitBlocks.join('\n\n');
                      }
                    }
                  }

                  if (isJunkBody(body)) body = '';

                  const parts = [];
                  if (title) parts.push('Título: ' + title);
                  if (company) parts.push('Empresa: ' + company);
                  if (location) parts.push('Ubicación: ' + location);
                  if (meta) parts.push('Condiciones\n' + meta);
                  if (body) parts.push('Descripción\n' + body);
                  if (requirements && !(body || '').includes(requirements.split('\n').slice(1).join('\n').trim()))
                    parts.push(requirements);
                  if (keywords) parts.push(keywords);
                  if (about) parts.push(about);
                  if (benefits) parts.push(benefits);

                  const result = norm(parts.join('\n\n'));
                  // Exigir cuerpo real: sin él devolvemos null para forzar otros intentos.
                  if (!body) return null;
                  return result.length >= 120 ? result : null;
                }
                """);

            if (!string.IsNullOrWhiteSpace(organized))
                return organized;
        }
        catch (PlaywrightException)
        {
            /* fallback abajo */
        }

        // Fallback por selectores (página completa primero).
        var fallbackParts = new List<string>();
        var meta = await FirstTextFromPageAsync(page,
        [
            "[div-link='oferta'] .mbB",
            "[div-link=\"oferta\"] .mbB",
            ".description_offer .fs14.mb10",
            "[description-offer] .fs14.mb10"
        ]);
        if (!string.IsNullOrWhiteSpace(meta) && meta.Length < 400)
            fallbackParts.Add("Condiciones\n" + meta.Trim());

        var body = await FirstTextFromPageAsync(page,
        [
            "[div-link='oferta'] p.mbB",
            "[div-link=\"oferta\"] p.mbB",
            ".description_offer .fs16.t_word_wrap",
            "[description-offer] .fs16.t_word_wrap",
            ".box_detail .fs16.t_word_wrap",
            "div.fs16.t_word_wrap"
        ]);
        if (!string.IsNullOrWhiteSpace(body) && body.Length >= 80)
            fallbackParts.Add("Descripción\n" + body.Trim());

        var reqList = await FirstTextFromPageAsync(page,
        [
            "[div-link='oferta'] ul.disc",
            "[div-link=\"oferta\"] ul.disc",
            ".description_offer ul.fs16.disc",
            "[description-offer] ul.fs16.disc",
            "ul.fs16.disc.mbB"
        ]);
        if (!string.IsNullOrWhiteSpace(reqList)
            && (body is null || !body.Contains(reqList, StringComparison.Ordinal)))
            fallbackParts.Add("Requerimientos\n" + reqList.Trim());

        if (fallbackParts.All(p => !p.StartsWith("Descripción", StringComparison.Ordinal)))
            return null;

        return string.Join("\n\n", fallbackParts);
    }

    private static async Task TryEnrichComputrabajoMetaAsync(IPage page, ScrapedOffer offer)
    {
        var meta = await FirstTextFromPageAsync(page,
        [
            "[div-link='oferta'] .mbB",
            "[div-link=\"oferta\"] .mbB",
            ".description_offer .fs14.mb10",
            "[description-offer] .fs14.mb10"
        ]);
        if (!string.IsNullOrWhiteSpace(meta))
        {
            offer.WorkModality ??= OfferFieldNormalizer.GuessModalityFromText(meta);
            offer.ContractType ??= OfferFieldNormalizer.GuessContractFromText(meta);
        }

        if (LooksLikeGarbageLocation(offer.Location))
        {
            var city = await FirstTextFromPageAsync(page,
            [
                ".header_detail p.fs16.mb5",
                ".box_detail .header_detail p.fs16.mb5",
                "p.fs16.mb5"
            ]);
            if (!string.IsNullOrWhiteSpace(city) && city.Length < 120)
                offer.Location = Clean(city);
        }

        var company = await FirstTextFromPageAsync(page,
        [
            ".header_detail a.dIB.mr10",
            ".header_detail a[href*='/empleos']",
            "p.fs16 a.dIB"
        ]);
        if (!string.IsNullOrWhiteSpace(company)
            && (string.IsNullOrWhiteSpace(offer.Company)
                || offer.Company.Contains("Importante empresa", StringComparison.OrdinalIgnoreCase)))
        {
            offer.Company = Clean(company);
        }
    }

    private static bool LooksLikeGarbageLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return true;
        var t = location.Trim();
        if (t.Contains('$') || t.Contains("Mensual", StringComparison.OrdinalIgnoreCase))
            return true;
        if (t.StartsWith("Hace ", StringComparison.OrdinalIgnoreCase))
            return true;
        if (t.Contains("Presencial", StringComparison.OrdinalIgnoreCase)
            && t.Contains("remoto", StringComparison.OrdinalIgnoreCase)
            && t.Length < 40)
            return true;
        return false;
    }

    private static bool IsWeakComputrabajoDescription(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;
        var t = text.Trim();
        if (t.Length < 200)
            return true;

        var lower = t.ToLowerInvariant();

        // Basura típica de pestañas / breadcrumbs sin el párrafo de la vacante.
        if (lower.Contains("descripción\noferta", StringComparison.Ordinal)
            || lower.Contains("descripción oferta empresa", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(
                lower,
                @"descripci[oó]n\s*\n\s*oferta(\s*\n\s*empresa)?"))
            return true;

        // Extraer tramo tras "Descripción" si existe.
        var descIdx = lower.IndexOf("descripción", StringComparison.Ordinal);
        var bodyPart = descIdx >= 0 ? t[(descIdx + "descripción".Length)..].Trim() : t;
        if (bodyPart.Length < 120)
            return true;

        var bodyLower = bodyPart.ToLowerInvariant();
        if (System.Text.RegularExpressions.Regex.IsMatch(
                bodyLower.Replace('\n', ' '),
                @"^\s*oferta(\s*/?\s*empresa)?\s*$"))
            return true;

        // Solo requerimientos / universidad.
        if (bodyLower.StartsWith("requerimientos", StringComparison.Ordinal)
            || (bodyLower.Contains("educación mínima", StringComparison.Ordinal) && bodyPart.Length < 400))
            return true;

        return false;
    }

    private static string? PreferRicherDescription(string? current, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return current;
        if (string.IsNullOrWhiteSpace(current))
            return candidate;
        if (IsWeakComputrabajoDescription(current) && !IsWeakComputrabajoDescription(candidate))
            return candidate;
        return candidate.Length >= current.Length ? candidate : current;
    }

    private static string? ExtractComputrabajoOfferId(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var oi = GetQueryParam(uri, "oi");
            if (!string.IsNullOrWhiteSpace(oi))
                return oi;
            var leaf = uri.AbsolutePath.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
            // ...-en-leon-7C7D683061741D8261373E686DCF3405
            var dash = leaf.LastIndexOf('-');
            if (dash >= 0 && dash < leaf.Length - 8)
            {
                var id = leaf[(dash + 1)..];
                if (id.Length >= 16 && id.All(static c => Uri.IsHexDigit(c)))
                    return id;
            }
        }
        return null;
    }

    private static string? BuildComputrabajoPrintUrl(string offerUrl)
    {
        if (!Uri.TryCreate(offerUrl, UriKind.Absolute, out var uri))
            return null;
        var oi = ExtractComputrabajoOfferId(offerUrl);
        if (string.IsNullOrWhiteSpace(oi))
            return null;
        return $"{uri.Scheme}://{uri.Host}/OffersDetail/Print?oi={Uri.EscapeDataString(oi)}";
    }

    private static string TrimComputrabajoNoise(string text)
    {
        // Quitar evaluaciones / salarios de mercado; conservar Acerca de y Beneficios.
        var markers = new[]
        {
            "\nSalarios\n",
            "\nSalarios",
            "\nprofesionales recomiendan",
            "\nEvaluaciones",
            "\nMostrar las ",
            "\nMostrar los "
        };

        var cut = text.Length;
        foreach (var marker in markers)
        {
            var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx > 120 && idx < cut)
                cut = idx;
        }

        var trimmed = cut < text.Length ? text[..cut].Trim() : text;
        // Estrellas / ratings sueltos al final.
        trimmed = System.Text.RegularExpressions.Regex.Replace(
            trimmed,
            @"\n\d+(?:[.,]\d+)?\s*\n(?:Ambiente de trabajo|Salario y prestaciones|Oportunidades de carrera|Director general)[\s\S]*$",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return trimmed.Trim();
    }

    /// <summary>Limpia descripción preservando saltos de línea (organización por secciones).</summary>
    private static string? CleanDescription(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var lines = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(static line =>
                string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));

        var cleaned = string.Join('\n', lines).Trim();
        while (cleaned.Contains("\n\n\n", StringComparison.Ordinal))
            cleaned = cleaned.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);

        return cleaned.Length <= max ? cleaned : cleaned[..max];
    }

    private static IReadOnlyList<string> DefaultDescriptionSelectors(string url)
    {
        var host = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host.ToLowerInvariant()
            : string.Empty;

        if (host.Contains("indeed.", StringComparison.Ordinal))
        {
            return
            [
                "#jobDescriptionText",
                "#job-description",
                "[id*='jobDescription']",
                ".jobsearch-JobComponent-description",
                "[data-testid='jobsearch-JobComponent-description']"
            ];
        }

        if (host.Contains("linkedin.", StringComparison.Ordinal))
        {
            return
            [
                "[data-testid='expandable-text-box']",
                ".jobs-description__content",
                ".jobs-box__html-content",
                "#job-details",
                ".job-details-about-the-job-module__description",
                "article.jobs-description"
            ];
        }

                if (host.Contains("computrabajo.", StringComparison.Ordinal))
        {
            return
            [
                "[div-link='oferta'] p.mbB",
                "[div-link=\"oferta\"] p.mbB",
                "[div-link='oferta']",
                ".description_offer .fs16.t_word_wrap",
                "[description-offer] .fs16.t_word_wrap",
                ".description_offer",
                "[description-offer]",
                "div.fs16.t_word_wrap"
            ];
        }

        if (host.Contains("elempleo.", StringComparison.Ordinal))
        {
            return
            [
                ".description-text",
                ".offer-description",
                "#offer-description",
                ".ee-offer-description",
                ".detalle-oferta"
            ];
        }

        return
        [
            "[itemprop='description']",
            ".job-description",
            "#job-description",
            "main"
        ];
    }

    private static async Task<string?> FirstTextFromPageAsync(IPage page, IReadOnlyList<string> selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var el = await page.QuerySelectorAsync(selector);
                if (el is null) continue;
                var text = (await el.InnerTextAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
            catch (PlaywrightException)
            {
                /* selector inválido / contexto */
            }
        }
        return null;
    }

    private static string? CleanLong(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var cleaned = Clean(value);
        if (cleaned is null)
            return null;
        return cleaned.Length <= max ? cleaned : cleaned[..max];
    }

    private static async Task<string?> FirstTextAsync(IElementHandle root, IReadOnlyList<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var el = await root.QuerySelectorAsync(selector);
            if (el is null) continue;
            var text = (await el.InnerTextAsync())?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }

    private static async Task<string?> FirstAttrAsync(
        IElementHandle root, IReadOnlyList<string> selectors, string attr)
    {
        foreach (var selector in selectors)
        {
            var el = await root.QuerySelectorAsync(selector);
            if (el is null) continue;
            var value = await el.GetAttributeAsync(attr);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static string? GuessByKeywords(string text, IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(text) || keywords.Count == 0)
            return null;

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            foreach (var keyword in keywords)
            {
                if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase) && line.Length < 120)
                    return line;
            }
        }
        return null;
    }

    private static async Task<ScrapeResult> ScrapeWithHttpSmokeAsync(
        JobPortalDto portal, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("DailyTimeWorker/1.0");
        using var response = await http.GetAsync(portal.Url, cancellationToken);
        var ok = response.IsSuccessStatusCode;
        return new ScrapeResult
        {
            PortalId = portal.Id,
            PortalName = portal.Name,
            Status = ok ? "smoke_http" : "error",
            Message = ok
                ? "HTTP OK pero sin Playwright no se pueden extraer tarjetas."
                : $"HTTP {(int)response.StatusCode}",
            Offers = []
        };
    }

    private static ScrapeResult Fail(JobPortalDto portal, string message) => new()
    {
        PortalId = portal.Id,
        PortalName = portal.Name,
        Status = "error",
        Message = message,
        Offers = []
    };

    private static string? AbsoluteUrl(string baseUrl, string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return null;
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            return absolute.ToString();
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var root) &&
            Uri.TryCreate(root, href, out var combined))
            return combined.ToString();
        return href;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static bool IsDestroyedContext(Exception ex)
    {
        var text = ex.ToString();
        return text.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase)
               || text.Contains("most likely because of a navigation", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlaywrightMissing(Exception ex)
    {
        // Solo fallback a smoke HTTP si falta el browser; otros PlaywrightException
        // deben reportarse como error real (antes todo caía en smoke_http engañoso).
        var text = ex.ToString();
        return text.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Please run the following command to download new browsers", StringComparison.OrdinalIgnoreCase)
               || (text.Contains("playwright", StringComparison.OrdinalIgnoreCase)
                   && text.Contains("install", StringComparison.OrdinalIgnoreCase)
                   && text.Contains("chromium", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class SearchCountry
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    /// <summary>Host opcional (ej. www.computrabajo.es). Si falta, se usa el de la plantilla.</summary>
    public string? Host { get; init; }
    /// <summary>Texto de ubicación para la URL (LinkedIn suele preferir inglés: Mexico, Spain).</summary>
    public string? Location { get; init; }
    /// <summary>geoId de LinkedIn para fijar el país (más fiable que solo location=).</summary>
    public string? GeoId { get; init; }
}

public sealed class ScrapeConfig
{
    public IReadOnlyList<string> ListSelectors { get; init; } = [];
    public IReadOnlyList<string> TitleSelectors { get; init; } = ["a", "h2", "h3"];
    public IReadOnlyList<string> LinkSelectors { get; init; } = ["a"];
    public IReadOnlyList<string> CompanySelectors { get; init; } = [];
    public IReadOnlyList<string> LocationSelectors { get; init; } = [];
    public IReadOnlyList<string> SnippetSelectors { get; init; } = [];
    /// <summary>Selectores del cuerpo de la vacante en la página de detalle.</summary>
    public IReadOnlyList<string> DescriptionSelectors { get; init; } = [];
    /// <summary>Si true, tras filtrar abre cada URL de oferta para leer la descripción.</summary>
    public bool VisitDetailPages { get; init; } = true;
    /// <summary>Máximo de páginas de detalle a visitar por corrida de portal.</summary>
    public int MaxDetailPages { get; init; } = 25;
    public IReadOnlyList<string> CompanyKeywords { get; init; } = ["empresa", "company"];
    public IReadOnlyList<string> LocationKeywords { get; init; } = ["remoto", "remote", "híbrido", "ciudad"];
    public IReadOnlyList<string> SearchKeywords { get; init; } = [];
    /// <summary>Keywords extra por país (code: mx|es|co). Se suman a searchKeywords.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SearchKeywordsByCountry { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<SearchCountry> SearchCountries { get; init; } = [];
    public string? SearchUrlTemplate { get; init; }
    public string? WaitForSelector { get; init; }
    /// <summary>Selectores a clicar tras cargar (ej. filtro "Recientes" en elempleo).</summary>
    public IReadOnlyList<string> ClickBeforeExtract { get; init; } = [];
    public int WaitAfterClickMs { get; init; } = 1200;
    public int MaxItems { get; init; } = 40;
    public int ScrollTimes { get; init; } = 1;
    /// <summary>Si true, pagina resultados (Indeed: start=10,20…).</summary>
    public bool Paginate { get; init; }
    /// <summary>Tamaño de página Indeed (start se incrementa de a este valor).</summary>
    public int PaginationPageSize { get; init; } = 10;
    /// <summary>Máximo de páginas de búsqueda por keyword/país (incluye la primera).</summary>
    public int MaxSearchPages { get; init; } = 1;
    /// <summary>Selectores del texto/fecha de publicación en cada tarjeta.</summary>
    public IReadOnlyList<string> DateSelectors { get; init; } = [];
    public IReadOnlyList<string> ModalitySelectors { get; init; } = [];
    public IReadOnlyList<string> ContractSelectors { get; init; } = [];
    /// <summary>Palabras extra para excluir (además de las de inclusión/discapacidad por defecto).</summary>
    public IReadOnlyList<string> ExcludeKeywords { get; init; } = [];
    /// <summary>
    /// Tras enriquecer descripción: la oferta debe mencionar al menos una de estas señales
    /// (ej. .net, c#, asp.net). Vacío = no filtrar por contenido.
    /// </summary>
    public IReadOnlyList<string> RequireContentKeywords { get; init; } = [];
    /// <summary>Señales extra de contenido por país (se suman a requireContentKeywords).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> RequireContentKeywordsByCountry { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Máxima antigüedad en días. null = sin filtro (cada portal lo define).</summary>
    public int? MaxAgeDays { get; init; }
    /// <summary>Máxima antigüedad en horas (tiene prioridad sobre maxAgeDays si ambos vienen).</summary>
    public int? MaxAgeHours { get; init; }
    /// <summary>Si hay maxAgeDays/Hours y la fecha no se entiende, descartar la oferta.</summary>
    public bool DropIfDateUnknown { get; init; } = true;
    /// <summary>País por defecto del portal (ej. Colombia, México).</summary>
    public string? DefaultCountry { get; init; }
    /// <summary>Idioma por defecto (ej. es, en).</summary>
    public string? DefaultLanguage { get; init; }
    /// <summary>
    /// Códigos/nombres de país donde solo se aceptan ofertas Remoto o Híbrido (ej. mx, es).
    /// </summary>
    public IReadOnlyList<string> RemoteOrHybridOnlyCountryCodes { get; init; } = [];

    public static ScrapeConfig Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ScrapeConfig();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ScrapeConfig
            {
                ListSelectors = ReadSelectors(root, "listSelectors", "listSelector"),
                TitleSelectors = ReadSelectors(root, "titleSelectors", "titleSelector", ["a", "h2", "h3"]),
                LinkSelectors = ReadSelectors(root, "linkSelectors", "linkSelector", ["a"]),
                CompanySelectors = ReadSelectors(root, "companySelectors", "companySelector"),
                LocationSelectors = ReadSelectors(root, "locationSelectors", "locationSelector"),
                SnippetSelectors = ReadSelectors(root, "snippetSelectors", "snippetSelector"),
                DescriptionSelectors = ReadSelectors(root, "descriptionSelectors", "descriptionSelector"),
                VisitDetailPages = ReadBool(root, "visitDetailPages", true),
                MaxDetailPages = ReadInt(root, "maxDetailPages", 25),
                DateSelectors = ReadSelectors(root, "dateSelectors", "dateSelector"),
                ModalitySelectors = ReadSelectors(root, "modalitySelectors", "modalitySelector"),
                ContractSelectors = ReadSelectors(root, "contractSelectors", "contractSelector"),
                ExcludeKeywords = ReadSelectors(root, "excludeKeywords", null),
                RequireContentKeywords = ReadSelectors(root, "requireContentKeywords", null),
                RequireContentKeywordsByCountry = ReadCountryKeywordMap(root, "requireContentKeywordsByCountry"),
                CompanyKeywords = ReadSelectors(root, "companyKeywords", null, ["empresa", "company"]),
                LocationKeywords = ReadSelectors(root, "locationKeywords", null, ["remoto", "remote", "híbrido", "ciudad"]),
                SearchKeywords = ReadSelectors(root, "searchKeywords", null),
                SearchKeywordsByCountry = ReadCountryKeywordMap(root, "searchKeywordsByCountry"),
                SearchCountries = ReadCountries(root, "searchCountries"),
                SearchUrlTemplate = ReadString(root, "searchUrlTemplate"),
                WaitForSelector = ReadString(root, "waitForSelector"),
                ClickBeforeExtract = ReadSelectors(root, "clickBeforeExtract", "clickBeforeExtract"),
                WaitAfterClickMs = ReadInt(root, "waitAfterClickMs", 1200),
                MaxItems = ReadInt(root, "maxItems", 40),
                ScrollTimes = ReadInt(root, "scrollTimes", 1),
                Paginate = ReadBool(root, "paginate", false),
                PaginationPageSize = ReadInt(root, "paginationPageSize", 10),
                MaxSearchPages = ReadInt(root, "maxSearchPages", 1),
                MaxAgeDays = ReadNullableInt(root, "maxAgeDays"),
                MaxAgeHours = ReadNullableInt(root, "maxAgeHours"),
                DropIfDateUnknown = ReadBool(root, "dropIfDateUnknown", true),
                DefaultCountry = ReadString(root, "defaultCountry"),
                DefaultLanguage = ReadString(root, "defaultLanguage"),
                RemoteOrHybridOnlyCountryCodes = ReadSelectors(root, "remoteOrHybridOnlyCountryCodes", null)
            };
        }
        catch
        {
            return new ScrapeConfig();
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadCountryKeywordMap(
        JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var obj) || obj.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Array)
                continue;
            var list = new List<string>();
            foreach (var item in prop.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;
                var s = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    list.Add(s);
            }

            if (list.Count > 0)
                map[prop.Name.Trim()] = list;
        }

        return map;
    }

    private static IReadOnlyList<SearchCountry> ReadCountries(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<SearchCountry>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var code = item.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(code))
                    continue;
                list.Add(new SearchCountry
                {
                    Code = code,
                    Name = DefaultCountryName(code)
                });
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var codeObj = item.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String
                ? codeProp.GetString()?.Trim()
                : null;
            var nameObj = item.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString()?.Trim()
                : null;
            var hostObj = item.TryGetProperty("host", out var hostProp) && hostProp.ValueKind == JsonValueKind.String
                ? hostProp.GetString()?.Trim()
                : null;
            var locationObj = item.TryGetProperty("location", out var locProp) && locProp.ValueKind == JsonValueKind.String
                ? locProp.GetString()?.Trim()
                : null;
            var geoIdObj = item.TryGetProperty("geoId", out var geoProp) && geoProp.ValueKind == JsonValueKind.String
                ? geoProp.GetString()?.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(codeObj) && string.IsNullOrWhiteSpace(nameObj))
                continue;

            codeObj ??= "";
            nameObj = string.IsNullOrWhiteSpace(nameObj) ? DefaultCountryName(codeObj) : nameObj;
            geoIdObj = string.IsNullOrWhiteSpace(geoIdObj) ? DefaultLinkedInGeoId(codeObj) : geoIdObj;
            list.Add(new SearchCountry
            {
                Code = codeObj,
                Name = nameObj!,
                Host = hostObj,
                Location = locationObj,
                GeoId = geoIdObj
            });
        }

        return list;
    }

    private static string? DefaultLinkedInGeoId(string code) => code.Trim().ToLowerInvariant() switch
    {
        "co" => "100876405",
        "mx" => "103323778",
        "es" => "105646813",
        _ => null
    };

    private static string DefaultCountryName(string code) => code.Trim().ToLowerInvariant() switch
    {
        "co" => "Colombia",
        "mx" => "México",
        "ar" => "Argentina",
        "cl" => "Chile",
        "pe" => "Perú",
        "ec" => "Ecuador",
        "uy" => "Uruguay",
        "bo" => "Bolivia",
        "py" => "Paraguay",
        "ve" => "Venezuela",
        "cr" => "Costa Rica",
        "pa" => "Panamá",
        "gt" => "Guatemala",
        "hn" => "Honduras",
        "sv" => "El Salvador",
        "ni" => "Nicaragua",
        "do" => "República Dominicana",
        "es" => "España",
        _ => code.ToUpperInvariant()
    };

    private static IReadOnlyList<string> ReadSelectors(
        JsonElement root, string plural, string? singular, string[]? fallback = null)
    {
        var list = new List<string>();
        if (root.TryGetProperty(plural, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var value = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    list.Add(value);
            }
        }

        if (list.Count == 0 && singular is not null)
        {
            var single = ReadString(root, singular);
            if (!string.IsNullOrWhiteSpace(single))
                list.Add(single);
        }

        if (list.Count == 0 && fallback is not null)
            return fallback;

        return list;
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()?.Trim()
            : null;

    private static int ReadInt(JsonElement root, string name, int defaultValue)
    {
        if (!root.TryGetProperty(name, out var prop))
            return defaultValue;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
            return Math.Max(0, n);
        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed))
            return Math.Max(0, parsed);
        return defaultValue;
    }

    private static int? ReadNullableInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
            return Math.Max(0, n);
        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed))
            return Math.Max(0, parsed);
        return null;
    }

    private static bool ReadBool(JsonElement root, string name, bool defaultValue)
    {
        if (!root.TryGetProperty(name, out var prop))
            return defaultValue;
        if (prop.ValueKind == JsonValueKind.True) return true;
        if (prop.ValueKind == JsonValueKind.False) return false;
        if (prop.ValueKind == JsonValueKind.String
            && bool.TryParse(prop.GetString(), out var parsed))
            return parsed;
        return defaultValue;
    }
}
