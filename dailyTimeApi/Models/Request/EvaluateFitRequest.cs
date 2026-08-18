namespace dailyTimeApi.Models.Request;

public class EvaluateFitRequest
{
    /// <summary>Si viene, se usa su WorkExperienceId y Url para localizar la oferta.</summary>
    public int? JobApplicationId { get; set; }

    public int? WorkExperienceId { get; set; }
    public int? JobOfferId { get; set; }
    public string? OfferUrl { get; set; }
}
