using dailyTimeApi.Models.Entities;

namespace dailyTimeApi.Repository.Interfaces;

/// <summary>
/// Acceso con seguimiento (tracking) para la sincronización con Google: a diferencia del resto
/// de repositorios, aquí las entidades se leen para modificarlas dentro del mismo ciclo.
/// </summary>
public interface IGoogleCalendarRepository
{
    Task<GoogleCalendarAccount?> GetAccountAsync(CancellationToken cancellationToken = default);
    Task AddAccountAsync(GoogleCalendarAccount account, CancellationToken cancellationToken = default);
    void RemoveAccount(GoogleCalendarAccount account);

    Task<IReadOnlyList<TaskItem>> GetTasksInWindowAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetNotesInWindowAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);

    Task<TaskItem?> GetTaskByEventIdAsync(string googleEventId, CancellationToken cancellationToken = default);
    Task<Note?> GetNoteByEventIdAsync(string googleEventId, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetTaskByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Note?> GetNoteByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddTaskAsync(TaskItem entity, CancellationToken cancellationToken = default);
    void RemoveTask(TaskItem entity);
    void RemoveNote(Note entity);
    Task<bool> TaskHasChildrenAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NoteHasChildrenAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoogleSyncDeletion>> GetPendingDeletionsAsync(CancellationToken cancellationToken = default);
    Task AddDeletionAsync(GoogleSyncDeletion deletion, CancellationToken cancellationToken = default);
    void RemoveDeletion(GoogleSyncDeletion deletion);

    /// <summary>Limpia las marcas de sincronización de todos los elementos (al desconectar).</summary>
    Task<int> ClearSyncMarksAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
