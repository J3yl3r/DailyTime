namespace dailyTimeApi.Models.Response;

public class JobOfferResponse
{
    public int Id { get; set; }
    public int JobPortalId { get; set; }
    public string PortalName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Url { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public string? DescriptionSnippet { get; set; }
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? WorkModality { get; set; }
    public string? ContractType { get; set; }
    public string? TechStack { get; set; }
    public DateTime? PostedAt { get; set; }
    public string Status { get; set; } = "new";
    public int SortOrder { get; set; }
    public DateTime CapturedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpsertJobOffersResponse
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Total { get; set; }
}
