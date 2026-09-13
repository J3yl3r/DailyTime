namespace dailyTimeWorker.Services.Scheduling;

public interface IScrapeScheduleSignal
{
    /// <summary>Se cancela cuando hay que volver a leer el horario (la web guardó cambios).</summary>
    CancellationToken Token { get; }
    void Reload();
}

public sealed class ScrapeScheduleSignal : IScrapeScheduleSignal
{
    private readonly object _gate = new();
    private CancellationTokenSource _cts = new();

    public CancellationToken Token
    {
        get
        {
            lock (_gate)
                return _cts.Token;
        }
    }

    public void Reload()
    {
        CancellationTokenSource previous;
        lock (_gate)
        {
            previous = _cts;
            _cts = new CancellationTokenSource();
        }

        // No se hace Dispose: quien espera puede seguir consultando el token cancelado.
        previous.Cancel();
    }
}
