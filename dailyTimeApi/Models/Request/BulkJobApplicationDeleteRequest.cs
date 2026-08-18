namespace dailyTimeApi.Models.Request;

public class BulkJobApplicationDeleteRequest
{
    public List<int> Ids { get; set; } = [];
}
