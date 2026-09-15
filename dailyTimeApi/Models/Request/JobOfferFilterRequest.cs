namespace dailyTimeApi.Models.Request;

public class JobOfferFilterRequest
{
    public int? PortalId { get; set; }
    public List<int>? PortalIds { get; set; }

    public string? Status { get; set; }
    public List<string>? Statuses { get; set; }

    /// <summary>Búsqueda en título, empresa y ubicación (cargo).</summary>
    public string? Search { get; set; }

    public string? Country { get; set; }
    public List<string>? Countries { get; set; }

    public string? Language { get; set; }
    public List<string>? Languages { get; set; }

    public string? WorkModality { get; set; }
    public List<string>? WorkModalities { get; set; }

    public string? ContractType { get; set; }
    public List<string>? ContractTypes { get; set; }

    public string? TechStack { get; set; }
    public List<string>? TechStacks { get; set; }

    public DateTime? CapturedFrom { get; set; }
    public DateTime? CapturedTo { get; set; }
    public DateTime? PostedFrom { get; set; }
    public DateTime? PostedTo { get; set; }

    /// <summary>Prioridades A, B, C; "none" = sin puntaje.</summary>
    public List<string>? Tiers { get; set; }
}

public class BulkJobOfferStatusRequest
{
    public List<int> Ids { get; set; } = [];
    public string Status { get; set; } = string.Empty;
}

public class BulkJobOfferDeleteRequest
{
    public List<int> Ids { get; set; } = [];
}
