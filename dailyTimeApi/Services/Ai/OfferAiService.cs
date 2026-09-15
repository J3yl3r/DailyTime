using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Response;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Repository.Interfaces;
using dailyTimeApi.Services.Interfaces;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Services.Ai;

public interface IOfferAiService
{
    Task<OfferAiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Analiza las ofertas A/B pendientes respetando límite diario, pausas y ritmo.</summary>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Analiza (o reanaliza) una oferta ahora, sea cual sea su prioridad.</summary>
    Task<JobOfferResponse> AnalyzeOneAsync(int id, CancellationToken cancellationToken = default);

    void RequestProcessing();
}

/// <summary>
/// Orquesta el análisis con IA. La IA solo ajusta el puntaje y explica; nunca descarta ni cambia estados.
/// </summary>
public class OfferAiService : IOfferAiService
{
    /// <summary>Pausa entre llamadas para no superar el límite por minuto de la capa gratuita.</summary>
    private static readonly TimeSpan DelayBetweenCalls = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(30);

    private readonly IJobOfferRepository _offers;
    private readonly IOfferTriageService _triage;
    private readonly IOfferAnalyzer _analyzer;
    private readonly OfferAiState _state;
    private readonly IJobOfferService _jobOffers;
    private readonly ILogger<OfferAiService> _logger;

    public OfferAiService(
        IJobOfferRepository offers,
        IOfferTriageService triage,
        IOfferAnalyzer analyzer,
        OfferAiState state,
        IJobOfferService jobOffers,
        ILogger<OfferAiService> logger)
    {
        _offers = offers;
        _triage = triage;
        _analyzer = analyzer;
        _state = state;
        _jobOffers = jobOffers;
        _logger = logger;
    }

    public void RequestProcessing() => _state.RequestRun();

    public async Task<OfferAiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _triage.GetSettingsAsync(cancellationToken);
        var now = DateTime.UtcNow;
        _state.ClearPauseIfExpired(now);

        return new OfferAiStatusResponse
        {
            Configured = _analyzer.IsConfigured,
            Enabled = settings.AiEnabled,
            Model = settings.AiModel,
            DailyLimit = settings.AiDailyLimit,
            UsedToday = await _offers.CountAiAnalyzedSinceAsync(OfferAiClock.DayStartUtc(now), cancellationToken),
            Pending = await _offers.CountPendingAiAnalysisAsync(cancellationToken),
            IsRunning = _state.IsRunning,
            PausedUntil = _state.PausedUntilUtc,
            PauseReason = _state.PauseReason,
            LastError = _state.LastError,
            LastRunAt = _state.LastRunAt,
            ResetsAt = OfferAiClock.NextResetUtc(now)
        };
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var (settings, profile) = await _triage.GetContextAsync(cancellationToken);
        if (!settings.AiEnabled || !_analyzer.IsConfigured)
            return 0;

        var now = DateTime.UtcNow;
        _state.ClearPauseIfExpired(now);
        if (_state.PausedUntilUtc is not null)
            return 0;

        var remaining = settings.AiDailyLimit
                        - await _offers.CountAiAnalyzedSinceAsync(OfferAiClock.DayStartUtc(now), cancellationToken);
        var processed = 0;

        while (remaining > 0)
        {
            var offer = (await _offers.GetPendingAiAnalysisAsync(1, cancellationToken)).FirstOrDefault();
            if (offer is null)
                break;

            if (processed > 0)
                await Task.Delay(DelayBetweenCalls, cancellationToken);

            if (!await AnalyzeAndSaveAsync(offer, settings, profile, cancellationToken))
                break;

            processed++;
            remaining--;
        }

        if (remaining <= 0 && _state.PausedUntilUtc is null
            && await _offers.CountPendingAiAnalysisAsync(cancellationToken) > 0)
        {
            _state.Pause(OfferAiClock.NextResetUtc(DateTime.UtcNow),
                $"Se alcanzó el límite diario de {settings.AiDailyLimit} análisis.");
        }

        if (processed > 0)
            _logger.LogInformation("Análisis de IA: {Count} oferta(s) analizadas.", processed);
        return processed;
    }

    public async Task<JobOfferResponse> AnalyzeOneAsync(int id, CancellationToken cancellationToken = default)
    {
        var offer = await _offers.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Oferta {id} no encontrada.");
        if (!_analyzer.IsConfigured)
            throw new ValidationException("Falta la clave de Gemini: configúrala en user secrets como Gemini:ApiKey.");

        var (settings, profile) = await _triage.GetContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        _state.ClearPauseIfExpired(now);
        if (_state.PausedUntilUtc is { } until)
            throw new ValidationException($"El análisis con IA está en pausa hasta {until:u}. {_state.PauseReason}");
        if (await _offers.CountAiAnalyzedSinceAsync(OfferAiClock.DayStartUtc(now), cancellationToken) >= settings.AiDailyLimit)
            throw new ValidationException($"Se alcanzó el límite diario de {settings.AiDailyLimit} análisis.");

        try
        {
            if (!await AnalyzeAndSaveAsync(offer, settings, profile, cancellationToken))
                throw new ValidationException(_state.LastError ?? "No se pudo analizar la oferta.");
        }
        catch (OfferAiException error)
        {
            throw new ValidationException(error.Message);
        }

        if (offer.AiError is not null)
            throw new ValidationException(offer.AiError);

        return await _jobOffers.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>Devuelve false si hay que detener la pasada (cuota, clave inválida, sin conexión).</summary>
    private async Task<bool> AnalyzeAndSaveAsync(
        JobOffer offer, OfferTriageSettings settings, TriageProfile profile, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        try
        {
            var analysis = await _analyzer.AnalyzeAsync(offer, profile, settings.AiModel, cancellationToken);
            offer.AiAnalysis = OfferAiJson.Serialize(analysis);
            offer.AiError = null;
            _state.SetError(null);
        }
        catch (OfferAiQuotaException quota)
        {
            var until = quota.IsDaily
                ? OfferAiClock.NextResetUtc(now)
                : now + (quota.RetryAfter is { } retry && retry > MinRetryDelay ? retry : MinRetryDelay);
            _state.Pause(until, quota.Message);
            _state.SetError(quota.Message);
            _logger.LogWarning("Análisis de IA en pausa hasta {Until:u}: {Message}", until, quota.Message);
            return false;
        }
        catch (OfferAiException error) when (error.IsFatal)
        {
            _state.SetError(error.Message);
            _logger.LogWarning("Análisis de IA detenido: {Message}", error.Message);
            return false;
        }
        catch (OfferAiException error)
        {
            // Error propio de esta oferta: se registra y no se reintenta sola (se puede reanalizar a mano).
            offer.AiError = OfferAiJson.Truncate(error.Message, 300);
            _logger.LogWarning("No se pudo analizar la oferta {Id}: {Message}", offer.Id, error.Message);
        }

        offer.AiAnalyzedAt = now;
        offer.AiModel = settings.AiModel;
        OfferTriageService.ApplyScore(offer, OfferScorer.Evaluate(offer, profile, settings, now), now);
        await _offers.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Despierta con cada solicitud (captura, recálculo o botón) y procesa pendientes; no sondea.</summary>
public class OfferAiBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OfferAiState _state;
    private readonly ILogger<OfferAiBackgroundService> _logger;

    public OfferAiBackgroundService(
        IServiceScopeFactory scopeFactory, OfferAiState state, ILogger<OfferAiBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _state.RequestRun(); // una pasada al arrancar por si quedaron pendientes

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _state.WaitForRunRequestAsync(stoppingToken);
                _state.BeginRun();
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IOfferAiService>().ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el análisis de IA de ofertas.");
                _state.SetError(ex.Message);
            }
            finally
            {
                _state.EndRun(DateTime.UtcNow);
            }
        }
    }
}
