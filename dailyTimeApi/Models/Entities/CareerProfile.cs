namespace dailyTimeApi.Models.Entities;

public class CareerProfile
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
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? SalaryCurrency { get; set; }
    public string? SalaryPeriod { get; set; }
    public string? SalaryNotes { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CareerProfileLink> Links { get; set; } = new List<CareerProfileLink>();
    public ICollection<CareerProfileLanguage> Languages { get; set; } = new List<CareerProfileLanguage>();
    public ICollection<CareerProfileCountry> Countries { get; set; } = new List<CareerProfileCountry>();
    public ICollection<CareerProfileStack> Stacks { get; set; } = new List<CareerProfileStack>();
    public ICollection<CareerProfileStrength> Strengths { get; set; } = new List<CareerProfileStrength>();
    public ICollection<CareerProfileEducation> Education { get; set; } = new List<CareerProfileEducation>();
    public ICollection<CareerProfileCertification> Certifications { get; set; } = new List<CareerProfileCertification>();
    public ICollection<CareerCoverLetter> CoverLetters { get; set; } = new List<CareerCoverLetter>();
}
