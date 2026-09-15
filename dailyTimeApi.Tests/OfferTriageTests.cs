using dailyTimeApi.Exceptions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Triage;
using dailyTimeApi.Services;
using dailyTimeApi.Services.Triage;

namespace dailyTimeApi.Tests;

public class OfferTriageTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    private static JobOffer Offer(string status, string source) => new()
    {
        Title = "Desarrollador .NET",
        Status = status,
        StatusSource = source
    };

    private static OfferEvaluation Evaluation(string? reason) => new(80, "A", [], reason);

    [Fact]
    public void SystemNewOffer_WithReason_IsDiscarded()
    {
        var offer = Offer("new", JobOfferStatusSources.System);

        var change = OfferTriageService.ApplyEvaluation(offer, Evaluation("Presencial fuera de Colombia"), null, Now);

        Assert.Equal(OfferTriageService.TriageChange.Discarded, change);
        Assert.Equal("discarded", offer.Status);
        Assert.Equal("Presencial fuera de Colombia", offer.DiscardReason);
        Assert.Equal(80, offer.PriorityScore);
        Assert.Equal("A", offer.PriorityTier);
    }

    [Fact]
    public void DuplicateReason_IsUsedWhenNoRuleMatches()
    {
        var offer = Offer("new", JobOfferStatusSources.System);

        OfferTriageService.ApplyEvaluation(offer, Evaluation(null), "Duplicada de #7 en linkedin", Now);

        Assert.Equal("discarded", offer.Status);
        Assert.Equal("Duplicada de #7 en linkedin", offer.DiscardReason);
    }

    [Theory]
    [InlineData("seen")]
    [InlineData("applied")]
    [InlineData("new")]
    public void UserDecidedStatus_IsNeverChanged(string status)
    {
        var offer = Offer(status, JobOfferStatusSources.User);

        var change = OfferTriageService.ApplyEvaluation(offer, Evaluation("Fuera de tu perfil"), null, Now);

        Assert.Equal(OfferTriageService.TriageChange.ProtectedByUser, change);
        Assert.Equal(status, offer.Status);
        Assert.Null(offer.DiscardReason);
        Assert.Equal(80, offer.PriorityScore);
    }

    [Fact]
    public void SystemDiscarded_ThatNoLongerMatches_IsRestored()
    {
        var offer = Offer("discarded", JobOfferStatusSources.System);
        offer.DiscardReason = "Publicada hace 40 días (máximo 30)";

        var change = OfferTriageService.ApplyEvaluation(offer, Evaluation(null), null, Now);

        Assert.Equal(OfferTriageService.TriageChange.Restored, change);
        Assert.Equal("new", offer.Status);
        Assert.Null(offer.DiscardReason);
    }

    [Fact]
    public void UserDiscarded_StaysDiscarded()
    {
        var offer = Offer("discarded", JobOfferStatusSources.User);

        var change = OfferTriageService.ApplyEvaluation(offer, Evaluation(null), null, Now);

        Assert.Equal(OfferTriageService.TriageChange.None, change);
        Assert.Equal("discarded", offer.Status);
    }

    [Fact]
    public void NormalizeSettings_CleansListsAndValidatesThresholds()
    {
        var normalized = OfferTriageService.NormalizeSettings(new OfferTriageSettings
        {
            ExcludedTitleKeywords = [" SAP ", "sap", "", "php"],
            BlockedCompanies = ["  "]
        });

        Assert.Equal(["SAP", "php"], normalized.ExcludedTitleKeywords);
        Assert.Empty(normalized.BlockedCompanies);
        Assert.Throws<ValidationException>(() =>
            OfferTriageService.NormalizeSettings(new OfferTriageSettings { TierAMin = 50, TierBMin = 50 }));
    }

    [Fact]
    public void ExperienceYears_MergesOverlappingPeriods()
    {
        var years = TriageProfileBuilder.ExperienceYears(
        [
            (new DateOnly(2020, 1, 1), new DateOnly(2022, 1, 1), false),
            (new DateOnly(2021, 1, 1), new DateOnly(2023, 1, 1), false)
        ], new DateOnly(2026, 1, 1));

        Assert.Equal(3.0, years);
    }

    [Fact]
    public void BuildProfile_ResolvesCountriesStacksAndEnglish()
    {
        var profile = new CareerProfile
        {
            Location = "Colombia",
            Countries = [new CareerProfileCountry { Name = "Mexico" }],
            Stacks = [new CareerProfileStack { Name = ".NET" }],
            Languages = [new CareerProfileLanguage { Name = "Inglés", Level = "Básico A2" }]
        };
        var experience = new WorkExperience
        {
            StartDate = new DateOnly(2022, 1, 1),
            IsCurrent = true,
            Technologies =
            [
                new WorkExperienceTechnology { Technology = new CareerTechnology { Name = "ASP.NET Core" } },
                new WorkExperienceTechnology { Technology = new CareerTechnology { Name = "Docker" } }
            ]
        };

        var result = TriageProfileBuilder.Build(profile, [experience], new OfferTriageSettings(), new DateOnly(2026, 1, 1));

        Assert.Equal("Colombia", result.HomeCountry);
        Assert.Equal(["México", "Colombia"], result.AllowedCountries);
        Assert.Equal([".NET"], result.Stacks);
        Assert.Equal(4.0, result.ExperienceYears);
        Assert.Equal("Básico A2", result.EnglishLevel);
        Assert.False(result.HasAdvancedEnglish);
    }
}
