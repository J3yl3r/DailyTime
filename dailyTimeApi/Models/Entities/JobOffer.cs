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
    public DateTime CapturedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public JobPortal JobPortal { get; set; } = null!;
}
