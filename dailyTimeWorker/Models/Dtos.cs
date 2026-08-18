namespace dailyTimeWorker.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}

public class JobPortalDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? LoginUrl { get; set; }
    public string? Notes { get; set; }
    public string? ScrapeConfig { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
}

public class UpdateScrapeRunRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? OfferCount { get; set; }
    public int? SavedInserted { get; set; }
    public int? SavedUpdated { get; set; }
}

public record ScrapeResult
{
    public int PortalId { get; init; }
    public string PortalName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? PageTitle { get; init; }
    public IReadOnlyList<ScrapedOffer> Offers { get; init; } = [];
    public int SavedInserted { get; init; }
    public int SavedUpdated { get; init; }
}

public class ScrapedOffer
{
    public string Title { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Url { get; set; }
    public string? DescriptionSnippet { get; set; }
    /// <summary>Descripción completa desde la página de detalle.</summary>
    public string? Description { get; set; }
    public string? Source { get; set; }
    public string? ExternalKey { get; set; }
    /// <summary>Texto crudo de fecha en la tarjeta (ej. "Hace 2 días").</summary>
    public string? PostedAtText { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? WorkModality { get; set; }
    public string? ContractType { get; set; }
    /// <summary>Stacks detectados, ej. ".NET", "React, Next.js".</summary>
    public string? TechStack { get; set; }
}

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

public class UpsertJobOffersResponse
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Total { get; set; }
}

public class SendEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}

public class NotificationResult
{
    public bool Sent { get; set; }
    public string Message { get; set; } = string.Empty;
}
