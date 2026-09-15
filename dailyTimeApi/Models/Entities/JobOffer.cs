namespace dailyTimeApi.Models.Entities;

public class JobOffer
{
    public int Id { get; set; }
    public int JobPortalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Url { get; set; }
    /// <summary>Clave estable para deduplicar (normalmente la URL absoluta).</summary>
    public string ExternalKey { get; set; } = string.Empty;
    public string? DescriptionSnippet { get; set; }
    /// <summary>Descripción completa de la vacante (página de detalle).</summary>
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? WorkModality { get; set; }
    public string? ContractType { get; set; }
    /// <summary>Stacks detectados (.NET, React, Next.js, Java, JavaScript, …).</summary>
    public string? TechStack { get; set; }
    public DateTime? PostedAt { get; set; }
    /// <summary>new | seen | discarded | applied</summary>
    public string Status { get; set; } = "new";
    /// <summary>
    /// Quién fijó el estado actual: <see cref="JobOfferStatusSources.System"/> (reglas) o
    /// <see cref="JobOfferStatusSources.User"/>. Las reglas nunca cambian un estado decidido por el usuario.
    /// </summary>
    public string StatusSource { get; set; } = JobOfferStatusSources.System;
    /// <summary>Motivo del descarte automático; null si la descartó el usuario o no está descartada.</summary>
    public string? DiscardReason { get; set; }
    /// <summary>Puntaje de prioridad 0–100 calculado con reglas y perfil. Null = sin evaluar.</summary>
    public int? PriorityScore { get; set; }
    /// <summary>A | B | C según los umbrales configurados.</summary>
    public string? PriorityTier { get; set; }
    /// <summary>JSON con el desglose del puntaje (factores y puntos).</summary>
    public string? ScoreBreakdown { get; set; }
    public DateTime? ScoredAt { get; set; }
    /// <summary>Fijada al arrastrarla: se ordena por <see cref="SortOrder"/> por encima del puntaje.</summary>
    public bool IsPinned { get; set; }
    public int SortOrder { get; set; }
    public DateTime CapturedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public JobPortal JobPortal { get; set; } = null!;
}

public static class JobOfferStatusSources
{
    public const string System = "system";
    public const string User = "user";
}
