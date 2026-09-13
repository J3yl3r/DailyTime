using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace dailyTimeApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<WorkItemStatus> WorkItemStatuses => Set<WorkItemStatus>();
    public DbSet<WorkItemCategory> WorkItemCategories => Set<WorkItemCategory>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<VaultAccount> VaultAccounts => Set<VaultAccount>();
    public DbSet<VaultPassword> VaultPasswords => Set<VaultPassword>();
    public DbSet<VaultService> VaultServices => Set<VaultService>();
    public DbSet<CareerCompany> CareerCompanies => Set<CareerCompany>();
    public DbSet<CareerPosition> CareerPositions => Set<CareerPosition>();
    public DbSet<CareerLocation> CareerLocations => Set<CareerLocation>();
    public DbSet<CareerField> CareerFields => Set<CareerField>();
    public DbSet<CareerTechnology> CareerTechnologies => Set<CareerTechnology>();
    public DbSet<CareerApplicationStatus> CareerApplicationStatuses => Set<CareerApplicationStatus>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<WorkExperienceTechnology> WorkExperienceTechnologies => Set<WorkExperienceTechnology>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<JobPortal> JobPortals => Set<JobPortal>();
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<JobPortalScrapeLog> JobPortalScrapeLogs => Set<JobPortalScrapeLog>();
    public DbSet<ScrapeSchedule> ScrapeSchedules => Set<ScrapeSchedule>();
    public DbSet<CareerProfile> CareerProfiles => Set<CareerProfile>();
    public DbSet<CareerProfileLink> CareerProfileLinks => Set<CareerProfileLink>();
    public DbSet<CareerProfileLanguage> CareerProfileLanguages => Set<CareerProfileLanguage>();
    public DbSet<CareerProfileCountry> CareerProfileCountries => Set<CareerProfileCountry>();
    public DbSet<CareerProfileStack> CareerProfileStacks => Set<CareerProfileStack>();
    public DbSet<CareerProfileStrength> CareerProfileStrengths => Set<CareerProfileStrength>();
    public DbSet<CareerProfileEducation> CareerProfileEducations => Set<CareerProfileEducation>();
    public DbSet<CareerProfileCertification> CareerProfileCertifications => Set<CareerProfileCertification>();
    public DbSet<CareerCoverLetter> CareerCoverLetters => Set<CareerCoverLetter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
