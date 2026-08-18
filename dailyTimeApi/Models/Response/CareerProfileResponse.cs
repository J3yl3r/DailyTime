namespace dailyTimeApi.Models.Response;

public class CareerProfileResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Location { get; set; }
    public string? Timezone { get; set; }
    public string? Availability { get; set; }
    public string? PreferredModality { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public IReadOnlyList<CareerProfileLinkResponse> Links { get; set; } = [];
    public IReadOnlyList<CareerProfileLanguageResponse> Languages { get; set; } = [];
    public CareerProfileSalaryResponse Salary { get; set; } = new();
    public IReadOnlyList<string> PreferredCountries { get; set; } = [];
    public IReadOnlyList<string> PreferredStacks { get; set; } = [];
    public string? Summary { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = [];
    public IReadOnlyList<CareerProfileEducationResponse> Education { get; set; } = [];
    public IReadOnlyList<CareerProfileCertificationResponse> Certifications { get; set; } = [];
    public IReadOnlyList<CareerCoverLetterResponse> CoverLetters { get; set; } = [];
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CareerProfileSalaryResponse
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string? Currency { get; set; }
    public string? Period { get; set; }
    public string? Notes { get; set; }
}

public class CareerProfileLinkResponse
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class CareerProfileLanguageResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Level { get; set; }
}

public class CareerProfileEducationResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Place { get; set; }
    public string? Year { get; set; }
}

public class CareerProfileCertificationResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Year { get; set; }
}

public class CareerCoverLetterResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    public string? Stack { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
