namespace dailyTimeApi.Models.Response;

public class JobOfferMetaResponse
{
    public IReadOnlyList<string> Countries { get; set; } = [];
    public IReadOnlyList<string> Languages { get; set; } = [];
    public IReadOnlyList<string> WorkModalities { get; set; } = [];
    public IReadOnlyList<string> ContractTypes { get; set; } = [];
    public IReadOnlyList<string> TechStacks { get; set; } = [];
}

public class BulkJobOfferActionResponse
{
    public int Affected { get; set; }
}
