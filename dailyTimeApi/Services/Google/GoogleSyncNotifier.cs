using dailyTimeApi.Models.Entities;
using dailyTimeApi.Repository.Interfaces;

namespace dailyTimeApi.Services.Google;

/// <summary>
/// Puente entre los servicios de dominio y la sincronización: deja la lápida de un borrado y
/// pide una pasada. Los servicios de tareas y notas no necesitan saber nada más de Google.
/// </summary>
public interface IGoogleSyncNotifier
{
    /// <summary>
    /// Registra que hay que borrar en Google el evento de un elemento que se está eliminando.
    /// No guarda: se apoya en el <c>SaveChanges</c> del servicio que borra, para que el borrado
    /// local y la lápida entren en la misma transacción.
    /// </summary>
    Task EnqueueDeletionAsync(IGoogleSyncedItem item, CancellationToken cancellationToken = default);

    /// <summary>Pide una sincronización cuanto antes (no espera a que termine).</summary>
    void RequestSync();
}

public class GoogleSyncNotifier : IGoogleSyncNotifier
{
    private readonly IGoogleCalendarRepository _repository;
    private readonly GoogleSyncState _state;

    public GoogleSyncNotifier(IGoogleCalendarRepository repository, GoogleSyncState state)
    {
        _repository = repository;
        _state = state;
    }

    public async Task EnqueueDeletionAsync(
        IGoogleSyncedItem item, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(item.GoogleEventId))
            return;

        var account = await _repository.GetAccountAsync(cancellationToken);
        if (account is null)
            return;

        await _repository.AddDeletionAsync(new GoogleSyncDeletion
        {
            GoogleEventId = item.GoogleEventId!,
            CalendarId = account.CalendarId,
            DeletedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public void RequestSync() => _state.RequestRun();
}
