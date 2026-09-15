using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Tests;

public class OfferScorerTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly TriageProfile Profile = new(
        HomeCountry: "Colombia",
        AllowedCountries: ["Colombia", "México", "España"],
        Stacks: [".NET", "C#", "React", "TypeScript", "SQL Server", "Power BI"],
        ExperienceYears: 4,
        EnglishLevel: "Basico A2",
        HasAdvancedEnglish: false);

    private static JobOffer Offer(
        string title,
        string? description = null,
        string? country = "Colombia",
        string? modality = "Remoto",
        string? company = "Acme",
        string? techStack = null,
        int ageDays = 1,
        string? contract = null) => new()
    {
        Title = title,
        Description = description,
        Country = country,
        WorkModality = modality,
        Company = company,
        TechStack = techStack,
        ContractType = contract,
        PostedAt = Now.AddDays(-ageDays),
        CapturedAt = Now.AddDays(-ageDays),
        Status = "new",
        StatusSource = JobOfferStatusSources.System
    };

    private static OfferEvaluation Evaluate(
        JobOffer offer, OfferTriageSettings? settings = null, TriageProfile? profile = null) =>
        OfferScorer.Evaluate(offer, profile ?? Profile, settings ?? new OfferTriageSettings(), Now);

    [Fact]
    public void DotNetRemoteInHomeCountry_IsTierAAndKept()
    {
        var result = Evaluate(Offer(
            "Desarrollador .NET Senior",
            "Buscamos 3 años de experiencia con C# y React.",
            techStack: ".NET, React"));

        Assert.Null(result.DiscardReason);
        Assert.Equal(92, result.Score);
        Assert.Equal("A", result.Tier);
        Assert.Equal(36, result.Factors.Single(f => f.Key == "stack").Points);
        Assert.Equal(11, result.Factors.Single(f => f.Key == "seniority").Points);
    }

    [Fact]
    public void CustomThresholds_ChangeTier()
    {
        var settings = new OfferTriageSettings { TierAMin = 95, TierBMin = 60 };
        var result = Evaluate(Offer("Desarrollador .NET Senior", "3 años de experiencia con C# y React.", techStack: ".NET, React"), settings);

        Assert.Equal("B", result.Tier);
    }

    [Theory]
    [InlineData("Presencial", "Presencial fuera de Colombia")]
    [InlineData("Híbrido", "Híbrido fuera de Colombia")]
    public void OnsiteOrHybridAbroad_IsDiscarded(string modality, string reason)
    {
        var result = Evaluate(Offer("Desarrollador .NET", country: "México", modality: modality));

        Assert.Equal(reason, result.DiscardReason);
    }

    [Fact]
    public void OnsiteInHomeCountry_IsDiscardedByDefault()
    {
        Assert.Equal("Presencial", Evaluate(Offer("Desarrollador .NET", modality: "Presencial")).DiscardReason);

        var allowOnsite = new OfferTriageSettings { DiscardOnsiteLocal = false };
        Assert.Null(Evaluate(Offer("Desarrollador .NET", modality: "Presencial"), allowOnsite).DiscardReason);
    }

    [Fact]
    public void ForeignCountryInTitle_IsDiscarded()
    {
        var result = Evaluate(Offer(
            "Senior Software Engineer, Content Asset Management - Czechia", country: "España", modality: "Remoto"));

        Assert.Equal("El título indica otro país: Chequia", result.DiscardReason);
    }

    [Theory]
    [InlineData("Backend Developer (Spain)")]
    [InlineData("Desarrollador .NET para cliente en Estados Unidos - remoto desde Colombia")]
    [InlineData("Senior .NET Developer - LATAM (Ireland based company)")]
    public void AllowedOrRemoteFromHomeCountryInTitle_IsKept(string title)
    {
        Assert.Null(Evaluate(Offer(title, techStack: ".NET")).DiscardReason);
    }

    [Fact]
    public void RemoteInAllowedCountry_IsKept()
    {
        var result = Evaluate(Offer("Desarrollador .NET", country: "México", modality: "Remoto"));

        Assert.Null(result.DiscardReason);
        Assert.Equal(7, result.Factors.Single(f => f.Key == "country").Points);
    }

    [Fact]
    public void CountryNotAllowed_IsDiscarded()
    {
        var result = Evaluate(Offer("Desarrollador .NET", country: "Argentina", modality: "Remoto"));

        Assert.Equal("País no aceptado: Argentina", result.DiscardReason);
    }

    [Fact]
    public void ResidencyAbroad_IsDiscarded()
    {
        var result = Evaluate(Offer(
            "Semi Senior Fullstack .NET & Vue.js Developer - México Only", country: "México", modality: "Remoto"));

        Assert.Equal("Exige residir en México", result.DiscardReason);
    }

    [Fact]
    public void ResidencyInHomeCountry_IsKept()
    {
        var result = Evaluate(Offer("Desarrollador .NET", "Solo para residentes en Colombia."));

        Assert.Null(result.DiscardReason);
    }

    [Fact]
    public void TechnologyKeyword_DiscardsUnlessTitleHasYourStack()
    {
        Assert.Equal("Palabra excluida en el título: «java»", Evaluate(Offer("Desarrollador Java")).DiscardReason);
        Assert.Null(Evaluate(Offer("Desarrollador Java / .NET")).DiscardReason);
    }

    [Fact]
    public void JavaKeyword_DoesNotMatchJavaScript()
    {
        var result = Evaluate(Offer("Desarrollador JavaScript", techStack: "JavaScript"));

        Assert.Null(result.DiscardReason);
    }

    [Fact]
    public void InternshipKeyword_DiscardsEvenWithYourStack()
    {
        var result = Evaluate(Offer("Practicante desarrollador .NET"));

        Assert.Equal("Palabra excluida en el título: «practicante»", result.DiscardReason);
    }

    [Fact]
    public void ExcludedKeyword_IgnoresContractType()
    {
        // LinkedIn marca empleos normales como «Prácticas»: solo cuenta el título.
        var result = Evaluate(Offer("Desarrollador/a Backend .NET", contract: "Prácticas"));

        Assert.Null(result.DiscardReason);
    }

    [Fact]
    public void OutOfProfile_IsDiscarded()
    {
        var result = Evaluate(Offer("Analista de Cobranza", "Gestión de cartera y cobro."));

        Assert.Equal("Fuera de tu perfil: sin tus stacks ni rol técnico", result.DiscardReason);
    }

    [Fact]
    public void TooOld_IsDiscarded()
    {
        var result = Evaluate(Offer("Desarrollador .NET", ageDays: 45));

        Assert.Equal("Publicada hace 45 días (máximo 30)", result.DiscardReason);
    }

    [Fact]
    public void BlockedCompany_IsDiscarded()
    {
        var settings = new OfferTriageSettings { BlockedCompanies = ["acme"] };

        var result = Evaluate(Offer("Desarrollador .NET", company: "ACME S.A.S."), settings);

        Assert.Equal("Empresa bloqueada: acme", result.DiscardReason);
    }

    [Fact]
    public void AutoDiscardDisabled_NeverDiscardsButStillScores()
    {
        var settings = new OfferTriageSettings { AutoDiscardEnabled = false };

        var result = Evaluate(Offer("Desarrollador .NET", country: "México", modality: "Presencial"), settings);

        Assert.Null(result.DiscardReason);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public void AdvancedEnglish_IsPenalizedForBasicLevel()
    {
        var result = Evaluate(Offer("Desarrollador .NET", "Requisito: inglés avanzado (B2+)."));

        var english = result.Factors.Single(f => f.Key == "english");
        Assert.Equal(OfferScorer.EnglishPenalty, english.Points);
        Assert.Contains("Basico A2", english.Detail);
    }

    [Fact]
    public void AdvancedEnglish_IsNotPenalizedWhenProfileIsAdvanced()
    {
        var profile = Profile with { EnglishLevel = "C1", HasAdvancedEnglish = true };

        var result = Evaluate(Offer("Desarrollador .NET", "Fluent English required."), profile: profile);

        Assert.DoesNotContain(result.Factors, f => f.Key == "english");
    }

    [Fact]
    public void SeniorRoleFarAboveExperience_ScoresNoSeniorityPoints()
    {
        var profile = Profile with { ExperienceYears = 1 };

        var result = Evaluate(Offer("Senior .NET Developer", "8+ years of experience with C#."), profile: profile);

        Assert.Equal(0, result.Factors.Single(f => f.Key == "seniority").Points);
    }

    [Fact]
    public void OtherStackWithoutYours_IsPenalized()
    {
        var result = Evaluate(Offer("Python Developer", techStack: "Python"));

        Assert.Null(result.DiscardReason);
        Assert.Equal(OfferScorer.OtherStackPenalty, result.Factors.Single(f => f.Key == "otherStack").Points);
    }

    [Fact]
    public void Normalize_HandlesDotNetCSharpAndJsSuffix()
    {
        Assert.Equal("asp dotnet core csharp y reactjs", OfferScorer.Normalize("ASP.NET Core / C# y React.js"));
    }

    [Theory]
    [InlineData("Mexico", "México")]
    [InlineData("Bogotá, Colombia", "Colombia")]
    [InlineData("SPAIN", "España")]
    [InlineData("Remote", null)]
    public void ResolveCountry_ReturnsCanonicalName(string input, string? expected)
    {
        Assert.Equal(expected, OfferScorer.ResolveCountry(input));
    }
}
