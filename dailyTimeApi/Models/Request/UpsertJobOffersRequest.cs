namespace dailyTimeApi.Models.Request;

public class UpsertJobOffersRequest
{
    public int JobPortalId { get; set; }
    public List<UpsertJobOfferItem> Offers { get; set; } = [];
}

public class UpsertJobOfferItem
{
    public string Title { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Url { get; set; }
    public string? ExternalKey { get; set; }
    public string? DescriptionSnippet { get; set; }
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? WorkModality { get; set; }
    public string? ContractType { get; set; }
    public string? TechStack { get; set; }
}

public class UpdateJobOfferStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
