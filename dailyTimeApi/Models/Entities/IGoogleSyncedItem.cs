namespace dailyTimeApi.Models.Entities;

/// <summary>
/// Campos de correlación con Google Calendar que comparten <see cref="TaskItem"/> y
/// <see cref="Note"/>, para que la sincronización trate a ambos igual.
/// </summary>
public interface IGoogleSyncedItem
{
    int Id { get; }
    string? GoogleEventId { get; set; }
    string? GoogleEtag { get; set; }

    /// <summary>Momento del último empuje o traída con éxito de este elemento.</summary>
    DateTime? GoogleSyncedAt { get; set; }

    /// <summary>Valor de <c>updated</c> que reportó Google la última vez, para detectar cambios remotos.</summary>
    DateTime? GoogleUpdatedAt { get; set; }

    /// <summary>local | google — dónde nació el elemento.</summary>
    string? SyncSource { get; set; }

    /// <summary>
    /// Color del evento en Google, ya resuelto a hexadecimal. Es el que el usuario eligió
    /// allí; si no eligió ninguno, el del calendario. Solo se rellena en lo que viene de Google.
    /// </summary>
    string? GoogleColor { get; set; }

    DateTime UpdatedAt { get; set; }
}
