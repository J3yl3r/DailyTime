namespace dailyTimeApi.Models.Request;

public class UpsertCareerProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? Location { get; set; }
    public string? Timezone { get; set; }
    public string? Availability { get; set; }
    public string? PreferredModality { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public CareerProfileSalaryRequest? Salary { get; set; }
    public string? Summary { get; set; }
    public CareerProfileLinkRequest[] Links { get; set; } = [];
    public CareerProfileLanguageRequest[] Languages { get; set; } = [];
    public string[] PreferredCountries { get; set; } = [];
    public string[] PreferredStacks { get; set; } = [];
    public string[] Strengths { get; set; } = [];
    public CareerProfileEducationRequest[] Education { get; set; } = [];
    public CareerProfileCertificationRequest[] Certifications { get; set; } = [];
    public CareerCoverLetterRequest[] CoverLetters { get; set; } = [];
}

public class CareerProfileSalaryRequest
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string? Currency { get; set; }
    public string? Period { get; set; }
    public string? Notes { get; set; }
}

public class CareerProfileLinkRequest
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class CareerProfileLanguageRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Level { get; set; }
}

public class CareerProfileEducationRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Place { get; set; }
    public string? Year { get; set; }
}

public class CareerProfileCertificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Year { get; set; }
}

public class CareerCoverLetterRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? Stack { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
