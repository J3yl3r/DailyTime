using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using dailyTimeApi.Models.Entities;
using dailyTimeApi.Models.Triage;

namespace dailyTimeApi.Services.Triage;

/// <summary>Un factor del puntaje. <see cref="MaxPoints"/> = 0 indica una penalización.</summary>
public sealed record ScoreFactor(string Key, string Label, int Points, int MaxPoints, string Detail);

public sealed record OfferEvaluation(
    int Score,
    string Tier,
    IReadOnlyList<ScoreFactor> Factors,
    string? DiscardReason);

/// <summary>
/// Perfil ya resuelto para evaluar ofertas. Solo país, stacks, años de experiencia e inglés:
/// ningún dato personal.
/// </summary>
public sealed record TriageProfile(
    string? HomeCountry,
    IReadOnlyList<string> AllowedCountries,
    IReadOnlyList<string> Stacks,
    double ExperienceYears,
    string? EnglishLevel,
    bool HasAdvancedEnglish)
{
    public static TriageProfile Empty { get; } = new(null, [], [], 0, null, false);
}

/// <summary>
/// Evaluación determinista de una oferta: puntaje 0–100 con desglose y, si aplica, motivo de descarte.
/// Es una función pura (sin BD) para poder probarla y recalcular en lote.
/// </summary>
public static class OfferScorer
{
    public const int StackMax = 40;
    public const int RoleMax = 10;
    public const int ModalityMax = 15;
    public const int CountryMax = 10;
    public const int SeniorityMax = 15;
    public const int RecencyMax = 10;
    public const int EnglishPenalty = -15;
    public const int OtherStackPenalty = -10;

    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CO");
    private static readonly Regex JsSuffix = new(@"([a-z]+)\.js(?![a-z0-9])", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Stacks conocidos con alias ya normalizados (ver <see cref="Normalize"/>).</summary>
    private static readonly (string Name, string[] Aliases)[] KnownStacks =
    [
        (".NET", ["dotnet", "net core", "net framework", "net developer", "desarrollador net", "asp dotnet", "aspnet", "entity framework", "ef core", "blazor"]),
        ("C#", ["csharp"]),
        ("TypeScript", ["typescript"]),
        ("React", ["react", "reactjs"]),
        ("Next.js", ["nextjs"]),
        ("JavaScript", ["javascript", "nodejs"]),
        ("SQL Server", ["sql server", "sqlserver", "mssql", "t sql", "tsql"]),
        ("Power BI", ["power bi", "powerbi"]),
        ("Power Platform", ["power platform", "power apps", "powerapps", "power automate"]),
        ("Azure", ["azure"]),
        ("Angular", ["angular"]),
        ("Vue", ["vue", "vuejs"]),
        ("Java", ["java", "spring boot"]),
        ("Python", ["python", "django", "fastapi"]),
        ("PHP", ["php", "laravel"]),
        ("Go", ["golang"]),
        ("Ruby", ["ruby", "rails"]),
        ("SAP", ["sap", "abap"]),
    ];

    private static readonly (string Name, string[] Aliases)[] KnownCountries =
    [
        ("Colombia", ["colombia"]),
        ("México", ["mexico"]),
        ("España", ["espana", "spain"]),
        ("Argentina", ["argentina"]),
        ("Chile", ["chile"]),
        ("Perú", ["peru"]),
        ("Ecuador", ["ecuador"]),
        ("Uruguay", ["uruguay"]),
        ("Venezuela", ["venezuela"]),
        ("Costa Rica", ["costa rica"]),
        ("Panamá", ["panama"]),
        ("Guatemala", ["guatemala"]),
        ("Brasil", ["brasil", "brazil"]),
        ("Estados Unidos", ["estados unidos", "united states", "usa", "eeuu"]),
        ("Canadá", ["canada"]),
        ("Portugal", ["portugal"]),
        ("Reino Unido", ["reino unido", "united kingdom", "uk"]),
        ("Alemania", ["alemania", "germany"]),
        ("Francia", ["francia", "france"]),
        ("Italia", ["italia", "italy"]),
        ("Países Bajos", ["paises bajos", "netherlands", "holanda"]),
        ("Chequia", ["chequia", "czechia", "czech republic", "republica checa"]),
        ("Polonia", ["polonia", "poland"]),
        ("Rumania", ["rumania", "romania"]),
        ("Irlanda", ["irlanda", "ireland"]),
        ("Suecia", ["suecia", "sweden"]),
        ("Suiza", ["suiza", "switzerland"]),
        ("Bélgica", ["belgica", "belgium"]),
        ("Austria", ["austria"]),
        ("Dinamarca", ["dinamarca", "denmark"]),
        ("Noruega", ["noruega", "norway"]),
        ("Finlandia", ["finlandia", "finland"]),
        ("India", ["india"]),
        ("Filipinas", ["filipinas", "philippines"]),
        ("Israel", ["israel"]),
        ("Bolivia", ["bolivia"]),
        ("Paraguay", ["paraguay"]),
        ("Honduras", ["honduras"]),
        ("El Salvador", ["el salvador"]),
        ("República Dominicana", ["republica dominicana", "dominican republic"]),
        ("Puerto Rico", ["puerto rico"]),
        ("Europa", ["europa", "europe", "union europea", "european union", "eu", "ue"]),
    ];

    /// <summary>Si el título trae alguno de estos, un país extranjero en el título no implica residir allá.</summary>
    private static readonly string[] RemoteFromHomeTerms =
        ["latam", "latinoamerica", "latin america", "remoto desde", "remote from", "desde colombia"];

    private static readonly string[] RoleTerms =
    [
        "desarrollador", "desarrolladora", "developer", "programador", "programadora", "programmer",
        "engineer", "ingeniero de software", "ingeniera de software", "ingeniero de desarrollo",
        "full stack", "fullstack", "backend", "back end", "frontend", "front end", "software",
        "arquitecto de software", "software architect", "devops", "analista desarrollador"
    ];

    private static readonly string[] RemoteTerms = ["remoto", "remote", "teletrabajo", "home office", "trabajo desde casa"];

    private static readonly Regex AdvancedEnglish = new(
        @"(?<![a-z])(?:ingles\W{0,3}(?:nivel\W{0,3})?(?:b2|c1|c2|avanzado|fluido|conversacional|intermedio avanzado)" +
        @"|(?:nivel\W{0,3})?(?:avanzado|b2|c1|c2)\W{0,3}de\W{0,3}ingles" +
        @"|english\W{0,3}(?:level\W{0,3})?(?:b2|c1|c2|advanced|fluent|proficiency)" +
        @"|(?:advanced|fluent|proficient|business)\W{0,3}(?:level\W{0,3})?(?:in\W{0,3})?english" +
        @"|bilingue|bilingual)(?![a-z])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YearsBefore = new(
        @"(?<![0-9])(\d{1,2})\s*\+?\s*(?:anos?|years?|yrs)\s*(?:o mas\s*|or more\s*|\+\s*)?(?:de\s*|of\s*)?(?:experiencia|experience)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YearsAfter = new(
        @"(?:experiencia|experience)\s*(?:minima\s*|laboral\s*|profesional\s*|comprobada\s*)?(?:de\s*|of\s*)?(?:al menos\s*|minimo\s*|at least\s*|mas de\s*)?(\d{1,2})\s*\+?\s*(?:anos?|years?)(?![a-z])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly (string Name, Regex Pattern)[] ResidencyPatterns =
        KnownCountries.Select(c => (c.Name, BuildResidencyRegex(c.Aliases))).ToArray();

    private enum Modality { Unknown, Remote, Hybrid, Onsite }

    private readonly record struct Level(int Years, string Label);

    public static OfferEvaluation Evaluate(
        JobOffer offer, TriageProfile profile, OfferTriageSettings settings, DateTime nowUtc)
    {
        var title = Normalize(offer.Title);
        var techStack = Normalize(offer.TechStack);
        var description = Normalize(offer.Description ?? offer.DescriptionSnippet);
        var body = $"{techStack} {description}";
        var all = $"{title} {body}";
        var factors = new List<ScoreFactor>();

        // Stack: lo que más separa una oferta útil de una irrelevante.
        var preferred = ResolveStacks(profile.Stacks);
        var titleHits = preferred.Where(s => ContainsAny(title, s.Aliases)).Select(s => s.Name).ToList();
        var bodyHits = preferred
            .Where(s => !titleHits.Contains(s.Name) && ContainsAny(body, s.Aliases))
            .Select(s => s.Name)
            .ToList();
        var anyStack = titleHits.Count + bodyHits.Count > 0;
        factors.Add(new ScoreFactor(
            "stack", "Tu stack",
            Math.Min(StackMax, titleHits.Count * 20 + bodyHits.Count * 8),
            StackMax,
            preferred.Count == 0
                ? "Agrega tus stacks en el perfil para evaluar el encaje."
                : anyStack ? DescribeHits(titleHits, bodyHits) : "No aparece ninguno de tus stacks."));

        if (preferred.Count > 0 && !anyStack)
        {
            var preferredNames = preferred.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var others = KnownStacks
                .Where(s => !preferredNames.Contains(s.Name)
                            && (ContainsAny(title, s.Aliases) || ContainsAny(techStack, s.Aliases)))
                .Select(s => s.Name)
                .ToList();
            if (others.Count > 0)
            {
                factors.Add(new ScoreFactor("otherStack", "Otro stack", OtherStackPenalty, 0,
                    $"Pide {string.Join(", ", others)}."));
            }
        }

        var roleInTitle = ContainsAny(title, RoleTerms);
        var roleInBody = !roleInTitle && ContainsAny(description, RoleTerms);
        factors.Add(new ScoreFactor(
            "role", "Rol técnico",
            roleInTitle ? RoleMax : roleInBody ? 4 : 0,
            RoleMax,
            roleInTitle ? "El título es de un rol de desarrollo."
                : roleInBody ? "El rol técnico solo aparece en la descripción."
                : "El título no es de un rol técnico."));

        var offerCountry = ResolveCountry(offer.Country) ?? ResolveCountry(offer.Location);
        var home = profile.HomeCountry;
        var abroad = offerCountry is not null && home is not null && offerCountry != home;
        int countryPoints;
        string countryDetail;
        if (offerCountry is null)
        {
            countryPoints = 5;
            countryDetail = "País no indicado.";
        }
        else if (home is null && profile.AllowedCountries.Count == 0)
        {
            countryPoints = 5;
            countryDetail = "Configura tu país en el perfil.";
        }
        else if (offerCountry == home)
        {
            countryPoints = CountryMax;
            countryDetail = $"En tu país ({offerCountry}).";
        }
        else if (profile.AllowedCountries.Contains(offerCountry))
        {
            countryPoints = 7;
            countryDetail = $"{offerCountry} está entre tus países.";
        }
        else
        {
            countryPoints = 0;
            countryDetail = $"{offerCountry} no está entre tus países.";
        }
        factors.Add(new ScoreFactor("country", "País", countryPoints, CountryMax, countryDetail));

        var modality = ResolveModality(offer.WorkModality);
        var (modalityPoints, modalityDetail) = modality switch
        {
            Modality.Remote => (ModalityMax, "Remoto."),
            Modality.Hybrid when abroad => (0, $"Híbrido en {offerCountry}."),
            Modality.Hybrid => (10, "Híbrido."),
            Modality.Onsite when abroad => (0, $"Presencial en {offerCountry}."),
            Modality.Onsite => (3, "Presencial."),
            _ when ContainsAny(all, RemoteTerms) => (12, "Remoto según la descripción."),
            _ => (6, "Modalidad no indicada.")
        };
        factors.Add(new ScoreFactor("modality", "Modalidad", modalityPoints, ModalityMax, modalityDetail));

        factors.Add(ScoreSeniority(all, title, profile.ExperienceYears));

        var reference = offer.PostedAt ?? offer.CapturedAt;
        var ageDays = Math.Max(0d, (nowUtc - reference).TotalDays);
        var days = (int)Math.Floor(ageDays);
        var (recencyPoints, recencyDetail) = ageDays switch
        {
            <= 1d => (RecencyMax, "Publicada hoy o ayer."),
            <= 3d => (8, $"Publicada hace {days} días."),
            <= 7d => (5, $"Publicada hace {days} días."),
            <= 14d => (2, $"Publicada hace {days} días."),
            _ => (0, $"Publicada hace {days} días.")
        };
        factors.Add(new ScoreFactor("recency", "Recencia", recencyPoints, RecencyMax, recencyDetail));

        if (settings.PenalizeEnglishGap && profile.EnglishLevel is not null && !profile.HasAdvancedEnglish
            && AdvancedEnglish.IsMatch(all))
        {
            factors.Add(new ScoreFactor("english", "Inglés", EnglishPenalty, 0,
                $"Pide inglés avanzado; tu nivel: {profile.EnglishLevel}."));
        }

        var score = Math.Clamp(factors.Sum(f => f.Points), 0, 100);
        var tier = score >= settings.TierAMin ? "A" : score >= settings.TierBMin ? "B" : "C";

        var discardReason = settings.AutoDiscardEnabled
            ? FindDiscardReason(offer, profile, settings, title, all, titleHits.Count > 0, anyStack,
                preferred.Count > 0, roleInTitle, offerCountry, abroad, modality, ageDays)
            : null;

        return new OfferEvaluation(score, tier, factors, discardReason);
    }

    private static string? FindDiscardReason(
        JobOffer offer,
        TriageProfile profile,
        OfferTriageSettings settings,
        string title,
        string all,
        bool stackInTitle,
        bool anyStack,
        bool hasPreferredStacks,
        bool roleInTitle,
        string? offerCountry,
        bool abroad,
        Modality modality,
        double ageDays)
    {
        var company = Normalize(offer.Company);
        foreach (var blocked in settings.BlockedCompanies)
        {
            var term = Normalize(blocked);
            if (term.Length > 0 && ContainsTerm(company, term))
                return $"Empresa bloqueada: {blocked}";
        }

        // Solo el título: LinkedIn marca empleos normales con tipo de contrato «Prácticas».
        foreach (var keyword in settings.ExcludedTitleKeywords)
        {
            var term = Normalize(keyword);
            if (term.Length == 0 || !ContainsTerm(title, term))
                continue;
            // "java" no descarta "Desarrollador Java/.NET": una tecnología cede si el título trae uno de tus stacks.
            if (stackInTitle && IsStackTerm(term))
                continue;
            return $"Palabra excluida en el título: «{keyword}»";
        }

        var home = profile.HomeCountry;
        if (settings.DiscardResidencyAbroad && home is not null)
        {
            var residency = ResidencyPatterns.Where(p => p.Pattern.IsMatch(all)).Select(p => p.Name).ToList();
            if (residency.Count > 0 && !residency.Contains(home))
                return $"Exige residir en {residency[0]}";

            // "Senior Engineer - Czechia": el portal pudo asignarla a otro país de búsqueda.
            var titleCountry = ForeignCountryInTitle(title, home, profile.AllowedCountries);
            if (titleCountry is not null)
                return $"El título indica otro país: {titleCountry}";
        }

        if (settings.DiscardOnsiteAbroad && abroad && modality is Modality.Onsite or Modality.Hybrid)
            return $"{(modality == Modality.Onsite ? "Presencial" : "Híbrido")} fuera de {home}";

        if (settings.DiscardOnsiteLocal && !abroad && modality == Modality.Onsite)
            return "Presencial";

        if (offerCountry is not null && profile.AllowedCountries.Count > 0
            && !profile.AllowedCountries.Contains(offerCountry))
            return $"País no aceptado: {offerCountry}";

        if (settings.MaxAgeDays > 0 && ageDays > settings.MaxAgeDays)
            return $"Publicada hace {(int)Math.Floor(ageDays)} días (máximo {settings.MaxAgeDays})";

        if (settings.DiscardOutOfProfile && hasPreferredStacks && !anyStack && !roleInTitle)
            return "Fuera de tu perfil: sin tus stacks ni rol técnico";

        return null;
    }

    private static ScoreFactor ScoreSeniority(string text, string title, double experienceYears)
    {
        var requiredYears = RequiredYears(text);
        var level = TitleLevel(title);
        if (requiredYears is null && level is null)
            return new ScoreFactor("seniority", "Seniority", 10, SeniorityMax, "Sin requisito claro de experiencia.");

        var required = Math.Max(requiredYears ?? 0, level?.Years ?? 0);
        var yearsText = experienceYears.ToString("0.#", Spanish);
        if (experienceYears <= 0)
        {
            return new ScoreFactor("seniority", "Seniority", 8, SeniorityMax,
                $"Pide ~{required} años; carga tus experiencias para compararlo.");
        }

        if (level?.Years == 1 && experienceYears >= 4)
            return new ScoreFactor("seniority", "Seniority", 9, SeniorityMax, $"Rol junior; tienes {yearsText} años.");

        var gap = required - experienceYears;
        var points = gap <= 0 ? SeniorityMax : gap <= 1 ? 11 : gap <= 3 ? 5 : 0;
        return new ScoreFactor("seniority", "Seniority", points, SeniorityMax,
            $"Pide ~{required} años ({level?.Label ?? "según la descripción"}); tienes {yearsText}.");
    }

    private static int? RequiredYears(string text)
    {
        int? max = null;
        foreach (var regex in new[] { YearsBefore, YearsAfter })
        {
            foreach (Match match in regex.Matches(text))
            {
                if (int.TryParse(match.Groups[1].Value, out var value) && value is >= 1 and <= 15)
                    max = Math.Max(max ?? 0, value);
            }
        }
        return max;
    }

    private static Level? TitleLevel(string title)
    {
        if (ContainsAny(title, ["semi senior", "semisenior", "ssr", "mid level"]))
            return new Level(3, "semi senior");
        if (ContainsAny(title, ["lead", "lider", "principal", "arquitecto", "architect", "head", "manager", "gerente", "director", "staff"]))
            return new Level(7, "líder");
        if (ContainsAny(title, ["senior", "sr"]))
            return new Level(5, "senior");
        if (ContainsAny(title, ["junior", "jr", "trainee", "entry level", "sin experiencia"]))
            return new Level(1, "junior");
        return null;
    }

    /// <summary>Minúsculas sin tildes, con "c#" → "csharp", ".net" → "dotnet", "react.js" → "reactjs" y separadores como espacios.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var formD = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length + 16);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var text = sb.ToString()
            .Replace("c#", " csharp ", StringComparison.Ordinal);
        text = JsSuffix.Replace(text, "$1js");
        text = text.Replace(".net", " dotnet", StringComparison.Ordinal)
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Replace('/', ' ');
        return Whitespace.Replace(text, " ").Trim();
    }

    /// <summary>Busca <paramref name="term"/> como palabra completa dentro de un texto ya normalizado.</summary>
    public static bool ContainsTerm(string text, string term)
    {
        if (text.Length == 0 || term.Length == 0)
            return false;

        var index = 0;
        while ((index = text.IndexOf(term, index, StringComparison.Ordinal)) >= 0)
        {
            var end = index + term.Length;
            var startsWord = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var endsWord = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (startsWord && endsWord)
                return true;
            index++;
        }
        return false;
    }

    /// <summary>Nombre canónico de un stack conocido (".NET", "React"…) o null si no se reconoce.</summary>
    public static string? ResolveKnownStack(string? name)
    {
        var normalized = Normalize(name);
        if (normalized.Length == 0)
            return null;

        foreach (var stack in KnownStacks)
        {
            if (Normalize(stack.Name) == normalized || ContainsAny(normalized, stack.Aliases))
                return stack.Name;
        }
        return null;
    }

    /// <summary>Nombre canónico de un país conocido ("México", "España"…) o null.</summary>
    public static string? ResolveCountry(string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
            return null;

        foreach (var country in KnownCountries)
        {
            if (Normalize(country.Name) == normalized || country.Aliases.Contains(normalized))
                return country.Name;
        }

        // Ubicaciones tipo "Bogotá, Colombia": solo alias largos para no confundir "uk" o "eu".
        foreach (var country in KnownCountries)
        {
            if (country.Aliases.Any(a => a.Length >= 4 && ContainsTerm(normalized, a)))
                return country.Name;
        }
        return null;
    }

    private static List<(string Name, string[] Aliases)> ResolveStacks(IEnumerable<string> names)
    {
        var result = new List<(string Name, string[] Aliases)>();
        foreach (var raw in names)
        {
            var normalized = Normalize(raw);
            if (normalized.Length == 0)
                continue;

            var known = ResolveKnownStack(raw);
            (string Name, string[] Aliases) entry = known is not null
                ? KnownStacks.First(s => s.Name == known)
                : (raw.Trim(), new[] { normalized });
            if (result.All(r => !string.Equals(r.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
                result.Add(entry);
        }
        return result;
    }

    private static string? ForeignCountryInTitle(string title, string home, IReadOnlyList<string> allowed)
    {
        var homeAliases = KnownCountries.FirstOrDefault(c => c.Name == home).Aliases ?? [];
        if (ContainsAny(title, homeAliases) || ContainsAny(title, RemoteFromHomeTerms))
            return null;

        foreach (var country in KnownCountries)
        {
            if (country.Name == home || allowed.Contains(country.Name))
                continue;
            // Alias cortos ("usa", "uk", "ue") chocan con palabras comunes: solo nombres completos.
            if (country.Aliases.Any(a => a.Length >= 4 && ContainsTerm(title, a)))
                return country.Name;
        }
        return null;
    }

    private static bool IsStackTerm(string term) =>
        KnownStacks.Any(s => Normalize(s.Name) == term || s.Aliases.Contains(term));

    private static bool ContainsAny(string text, IEnumerable<string> terms) =>
        terms.Any(term => ContainsTerm(text, term));

    private static Modality ResolveModality(string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Contains("remot") || normalized.Contains("teletrabajo"))
            return Modality.Remote;
        if (normalized.Contains("hibrid") || normalized.Contains("hybrid"))
            return Modality.Hybrid;
        if (normalized.Contains("presencial") || normalized.Contains("on site") || normalized.Contains("onsite") || normalized.Contains("en sitio"))
            return Modality.Onsite;
        return Modality.Unknown;
    }

    private static string DescribeHits(IReadOnlyList<string> titleHits, IReadOnlyList<string> bodyHits)
    {
        var parts = new List<string>();
        if (titleHits.Count > 0)
            parts.Add($"En el título: {string.Join(", ", titleHits)}");
        if (bodyHits.Count > 0)
            parts.Add($"En la descripción: {string.Join(", ", bodyHits)}");
        return string.Join(". ", parts) + ".";
    }

    private static Regex BuildResidencyRegex(IEnumerable<string> aliases)
    {
        var names = string.Join("|", aliases.Select(Regex.Escape));
        const string end = "(?![a-z0-9])";
        var pattern =
            $@"(?<![a-z0-9])(?:{names}) only{end}" +
            $@"|only (?:in|from|for|within) (?:{names}){end}" +
            $@"|(?:solo|solamente|unicamente) (?:para )?(?:candidatos |personas )?(?:residentes? |ubicad[oa]s? )?(?:en|de) (?:{names}){end}" +
            $@"|residentes? (?:en|de) (?:{names}){end}" +
            $@"|(?:residir|vivir|residencia) en (?:{names}){end}" +
            $@"|must (?:be |live |reside )(?:based |located )?in (?:{names}){end}" +
            $@"|candidates? (?:based|located|residing) in (?:{names}){end}" +
            $@"|(?:permiso de trabajo|work permit|autorizacion para trabajar|right to work) (?:en|in|para) (?:{names}){end}";
        return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
