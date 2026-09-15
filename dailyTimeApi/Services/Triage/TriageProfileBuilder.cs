using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Triage;

namespace dailyTimeApi.Services.Triage;

/// <summary>Arma el <see cref="TriageProfile"/> a partir del perfil de carrera, las experiencias y las reglas.</summary>
public static class TriageProfileBuilder
{
    private static readonly string[] AdvancedLevelTerms =
        ["b2", "c1", "c2", "avanzado", "advanced", "fluido", "fluent", "nativo", "native", "bilingue", "bilingual", "proficient"];

    public static TriageProfile Build(
        CareerProfile? profile,
        IEnumerable<WorkExperience> experiences,
        OfferTriageSettings settings,
        DateOnly today)
    {
        var experienceList = experiences.ToList();
        var home = OfferScorer.ResolveCountry(profile?.Location);

        IEnumerable<string> countrySource = settings.AllowedCountries.Count > 0
            ? settings.AllowedCountries
            : profile?.Countries.OrderBy(c => c.SortOrder).Select(c => c.Name) ?? Enumerable.Empty<string>();
        var allowed = new List<string>();
        foreach (var raw in countrySource.Append(home ?? string.Empty))
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var name = OfferScorer.ResolveCountry(raw) ?? raw.Trim();
            if (!allowed.Contains(name, StringComparer.OrdinalIgnoreCase))
                allowed.Add(name);
        }

        var stacks = new List<string>();
        void AddStack(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            var trimmed = name.Trim();
            if (!stacks.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                stacks.Add(trimmed);
        }

        foreach (var stack in profile?.Stacks.OrderBy(s => s.SortOrder) ?? Enumerable.Empty<CareerProfileStack>())
            AddStack(stack.Name);
        // De las experiencias solo cuentan tecnologías reconocidas: "Git" o "Scrum" no deben inflar el encaje.
        foreach (var technology in experienceList.SelectMany(e => e.Technologies).Select(t => t.Technology?.Name))
            AddStack(OfferScorer.ResolveKnownStack(technology));

        var years = ExperienceYears(experienceList.Select(e => (e.StartDate, e.EndDate, e.IsCurrent)), today);

        var english = profile?.Languages.FirstOrDefault(l =>
        {
            var name = OfferScorer.Normalize(l.Name);
            return name.Contains("ingles") || name.Contains("english");
        });
        var level = string.IsNullOrWhiteSpace(english?.Level) ? null : english.Level.Trim();
        var normalizedLevel = OfferScorer.Normalize(level);
        var advanced = AdvancedLevelTerms.Any(term => OfferScorer.ContainsTerm(normalizedLevel, term));

        return new TriageProfile(home, allowed, stacks, years, level, advanced)
        {
            PreferredModality = profile?.PreferredModality?.Trim() is { Length: > 0 } preferred ? preferred : null
        };
    }

    /// <summary>Años de experiencia sumando periodos sin contar dos veces los que se solapan.</summary>
    public static double ExperienceYears(
        IEnumerable<(DateOnly Start, DateOnly? End, bool IsCurrent)> ranges, DateOnly today)
    {
        var intervals = ranges
            .Select(r => (r.Start, End: r.IsCurrent || r.End is null ? today : r.End.Value))
            .Where(r => r.End > r.Start)
            .OrderBy(r => r.Start)
            .ToList();

        var totalDays = 0;
        DateOnly? currentStart = null;
        DateOnly? currentEnd = null;
        foreach (var (start, end) in intervals)
        {
            if (currentEnd is null || start > currentEnd.Value)
            {
                if (currentStart is not null && currentEnd is not null)
                    totalDays += currentEnd.Value.DayNumber - currentStart.Value.DayNumber;
                currentStart = start;
                currentEnd = end;
            }
            else if (end > currentEnd.Value)
            {
                currentEnd = end;
            }
        }

        if (currentStart is not null && currentEnd is not null)
            totalDays += currentEnd.Value.DayNumber - currentStart.Value.DayNumber;

        return Math.Round(totalDays / 365.25, 1);
    }
}
