using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Services.Ai;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Tests;

public class OfferAiTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private static JobOffer Offer(string? aiAnalysis = null) => new()
    {
        Title = "Desarrollador .NET",
        Company = "Acme",
        Country = "Colombia",
        WorkModality = "Remoto",
        Description = "Buscamos desarrollador con C# y SQL Server.",
        PostedAt = Now.AddDays(-1),
        CapturedAt = Now.AddDays(-1),
        Status = "new",
        StatusSource = JobOfferStatusSources.System,
        AiAnalysis = aiAnalysis
    };

    private static readonly TriageProfile Profile = new("Colombia", ["Colombia"], [".NET", "C#"], 3, "Basico A2", false);

    [Theory]
    [InlineData("apply", 0, 10)]
    [InlineData("maybe", 0, 0)]
    [InlineData("skip", 0, -15)]
    [InlineData("skip", 2, -20)]
    public void AiFactor_AdjustsScoreByVerdictAndMissingRequirements(string verdict, int missing, int expected)
    {
        var analysis = new OfferAiAnalysis
        {
            Verdict = verdict,
            Summary = "Resumen",
            MissingMustHaves = Enumerable.Range(1, missing).Select(i => $"Requisito {i}").ToList()
        };

        var result = OfferScorer.Evaluate(Offer(OfferAiJson.Serialize(analysis)), Profile, new OfferTriageSettings(), Now);

        Assert.Equal(expected, result.Factors.Single(f => f.Key == "ai").Points);
    }

    [Fact]
    public void AiFactor_IsIgnoredWithoutValidAnalysis()
    {
        var withoutAnalysis = OfferScorer.Evaluate(Offer(), Profile, new OfferTriageSettings(), Now);
        var corrupt = OfferScorer.Evaluate(Offer("{no es json"), Profile, new OfferTriageSettings(), Now);

        Assert.DoesNotContain(withoutAnalysis.Factors, f => f.Key == "ai");
        Assert.DoesNotContain(corrupt.Factors, f => f.Key == "ai");
    }

    [Fact]
    public void ParseAnalysis_ReadsTextPartsIgnoringThoughtsAndSanitizes()
    {
        const string body = """
            {
              "candidates": [{
                "content": {
                  "role": "model",
                  "parts": [
                    { "text": "razonando...", "thought": true },
                    { "text": "{\"verdict\":\"APPLY\",\"summary\":\"Encaja con .NET\",\"mandatoryRequirements\":[{\"requirement\":\".NET 8\",\"met\":\"yes\"},{\"requirement\":\"Kafka\",\"met\":\"quizas\"}],\"missingMustHaves\":[],\"seniority\":\"expert\",\"requiredYears\":99,\"englishLevel\":\"basic\",\"workModality\":\"remote\",\"locationRestriction\":\"\",\"salary\":{\"min\":-5,\"max\":3000,\"currency\":\"usd\",\"period\":\"month\"},\"redFlags\":[\"a\",\"b\",\"c\",\"d\",\"e\",\"f\"]}" }
                  ]
                },
                "finishReason": "STOP"
              }],
              "usageMetadata": { "promptTokenCount": 120, "candidatesTokenCount": 80 }
            }
            """;

        var analysis = GeminiResponseParser.ParseAnalysis(body);

        Assert.Equal("apply", analysis.Verdict);
        Assert.Equal("unknown", analysis.Seniority);
        Assert.Equal(30, analysis.RequiredYears);
        Assert.Equal("unknown", analysis.MandatoryRequirements[1].Met);
        Assert.Equal(0, analysis.Salary.Min);
        Assert.Equal("USD", analysis.Salary.Currency);
        Assert.Equal(5, analysis.RedFlags.Count);
    }

    [Fact]
    public void ParseAnalysis_ExplainsBlockedPrompt()
    {
        var error = Assert.Throws<OfferAiException>(() =>
            GeminiResponseParser.ParseAnalysis("""{ "promptFeedback": { "blockReason": "SAFETY" } }"""));

        Assert.Contains("SAFETY", error.Message);
        Assert.False(error.IsFatal);
    }

    [Theory]
    [InlineData("GenerateRequestsPerMinutePerProjectPerModel-FreeTier", false)]
    [InlineData("GenerateRequestsPerDayPerProjectPerModel-FreeTier", true)]
    public void ParseError_DetectsQuotaKindAndRetryDelay(string quotaId, bool daily)
    {
        var body = $$"""
            {
              "error": {
                "code": 429,
                "message": "You exceeded your current quota.",
                "status": "RESOURCE_EXHAUSTED",
                "details": [
                  { "@type": "type.googleapis.com/google.rpc.QuotaFailure", "violations": [ { "quotaId": "{{quotaId}}" } ] },
                  { "@type": "type.googleapis.com/google.rpc.RetryInfo", "retryDelay": "37s" }
                ]
              }
            }
            """;

        var error = Assert.IsType<OfferAiQuotaException>(GeminiResponseParser.ParseError(429, body));

        Assert.Equal(daily, error.IsDaily);
        Assert.Equal(TimeSpan.FromSeconds(37), error.RetryAfter);
        Assert.True(error.IsFatal);
    }

    [Fact]
    public void ParseError_InvalidKeyIsFatal()
    {
        var error = GeminiResponseParser.ParseError(400,
            """{ "error": { "code": 400, "message": "API key not valid. Please pass a valid API key.", "status": "INVALID_ARGUMENT" } }""");

        Assert.IsNotType<OfferAiQuotaException>(error);
        Assert.True(error.IsFatal);
    }

    [Fact]
    public void Prompt_NeverIncludesPersonalData()
    {
        var careerProfile = new CareerProfile
        {
            FullName = "Juana Pérez",
            Email = "juana@example.com",
            Phone = "+57 300 123 4567",
            SalaryMin = 1234,
            SalaryMax = 5678,
            SalaryNotes = "Mínimo negociable",
            Location = "Colombia",
            PreferredModality = "Remoto o híbrido",
            Stacks = [new CareerProfileStack { Name = ".NET" }],
            Languages = [new CareerProfileLanguage { Name = "Inglés", Level = "Básico A2" }]
        };
        var profile = TriageProfileBuilder.Build(careerProfile, [], new OfferTriageSettings(), new DateOnly(2026, 9, 14));

        var prompt = OfferAiPrompt.BuildUserPrompt(Offer(), profile);

        Assert.DoesNotContain("Juana", prompt);
        Assert.DoesNotContain("juana@example.com", prompt);
        Assert.DoesNotContain("300 123", prompt);
        Assert.DoesNotContain("1234", prompt);
        Assert.DoesNotContain("5678", prompt);
        Assert.DoesNotContain("negociable", prompt);
        Assert.Contains(".NET", prompt);
        Assert.Contains("Remoto o híbrido", prompt);
        Assert.Contains("Básico A2", prompt);
    }

    [Fact]
    public void BuildRequest_AsksForSchemaJsonAndOptionalThinking()
    {
        var withThinking = GeminiOfferAnalyzer.BuildRequest(Offer(), Profile, includeThinking: true);
        var withoutThinking = GeminiOfferAnalyzer.BuildRequest(Offer(), Profile, includeThinking: false);

        var config = withThinking["generationConfig"]!;
        Assert.Equal("application/json", config["responseMimeType"]!.GetValue<string>());
        Assert.Equal("object", config["responseJsonSchema"]!["type"]!.GetValue<string>());
        Assert.Equal("low", config["thinkingConfig"]!["thinkingLevel"]!.GetValue<string>());
        Assert.Null(withoutThinking["generationConfig"]!["thinkingConfig"]);
    }

    [Fact]
    public void Clock_DailyQuotaResetsAtPacificMidnight()
    {
        // 14/09/2026 12:00 UTC = 05:00 en Los Ángeles (PDT, UTC-7).
        Assert.Equal(new DateTime(2026, 9, 14, 7, 0, 0, DateTimeKind.Utc), OfferAiClock.DayStartUtc(Now));
        Assert.Equal(new DateTime(2026, 9, 15, 7, 0, 0, DateTimeKind.Utc), OfferAiClock.NextResetUtc(Now));
    }
}
