namespace dailyTimeWorker.Services.Scraping;

public interface IScrapeRunCoordinator
{
    IReadOnlyList<int> RunningPortalIds { get; }
    CancellationToken Register(int portalId, CancellationToken requestToken);
    void Unregister(int portalId);
    bool CancelRunning();
}

/// <summary>
/// Coordina scrapes en curso para poder detenerlos desde la UI sin tumbar el worker.
/// </summary>
public sealed class ScrapeRunCoordinator : IScrapeRunCoordinator
{
    private readonly object _gate = new();
    private CancellationTokenSource _stopCts = new();
    private readonly HashSet<int> _running = [];

    public IReadOnlyList<int> RunningPortalIds
    {
        get
        {
            lock (_gate)
                return _running.ToList();
        }
    }

    public CancellationToken Register(int portalId, CancellationToken requestToken)
    {
        lock (_gate)
        {
            _running.Add(portalId);
            return CancellationTokenSource.CreateLinkedTokenSource(_stopCts.Token, requestToken).Token;
        }
    }

    public void Unregister(int portalId)
    {
        lock (_gate)
            _running.Remove(portalId);
    }

    public bool CancelRunning()
    {
        lock (_gate)
        {
            if (_running.Count == 0)
                return false;

            _stopCts.Cancel();
            _stopCts.Dispose();
            _stopCts = new CancellationTokenSource();
            return true;
        }
    }
}
