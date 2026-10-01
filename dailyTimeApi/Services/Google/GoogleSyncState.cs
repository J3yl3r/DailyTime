namespace dailyTimeApi.Services.Google;

/// <summary>
/// Estado compartido de la sincronización con Google. Sirve para pedir una pasada inmediata
/// (tras guardar una tarea, al cambiar los ajustes) sin sondear la base de datos.
/// </summary>
public class GoogleSyncState
{
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly object _gate = new();
    private bool _running;

    public bool IsRunning
    {
        get { lock (_gate) return _running; }
    }

    /// <summary>Pide una pasada cuanto antes. Varias peticiones seguidas se funden en una.</summary>
    public void RequestRun()
    {
        try
        {
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Ya había una pasada pedida: no hace falta encolar otra.
        }
    }

    /// <summary>Espera a que pidan una pasada o a que venza el intervalo automático.</summary>
    public Task WaitForRunRequestAsync(TimeSpan interval, CancellationToken cancellationToken) =>
        _signal.WaitAsync(interval, cancellationToken);

    public void BeginRun()
    {
        lock (_gate) _running = true;
    }

    public void EndRun()
    {
        lock (_gate) _running = false;
    }
}
