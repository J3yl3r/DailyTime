using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class CareerProfileConfiguration : IEntityTypeConfiguration<CareerProfile>
{
    public void Configure(EntityTypeBuilder<CareerProfile> builder)
    {
        builder.ToTable("CareerProfile");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Headline).HasMaxLength(300);
        builder.Property(x => x.Location).HasMaxLength(150);
        builder.Property(x => x.Timezone).HasMaxLength(80);
        builder.Property(x => x.Availability).HasMaxLength(120);
        builder.Property(x => x.PreferredModality).HasMaxLength(80);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.SalaryMin).HasPrecision(18, 2);
        builder.Property(x => x.SalaryMax).HasPrecision(18, 2);
        builder.Property(x => x.SalaryCurrency).HasMaxLength(10);
        builder.Property(x => x.SalaryPeriod).HasMaxLength(30);
        builder.Property(x => x.SalaryNotes).HasMaxLength(500);
        builder.Property(x => x.Summary); // nvarchar(max)
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasMany(x => x.Links)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Languages)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Countries)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Stacks)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Strengths)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Education)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Certifications)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.CoverLetters)
            .WithOne(x => x.CareerProfile)
            .HasForeignKey(x => x.CareerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CareerProfileLinkConfiguration : IEntityTypeConfiguration<CareerProfileLink>
{
    public void Configure(EntityTypeBuilder<CareerProfileLink> builder)
    {
        builder.ToTable("CareerProfileLink");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerProfileLanguageConfiguration : IEntityTypeConfiguration<CareerProfileLanguage>
{
    public void Configure(EntityTypeBuilder<CareerProfileLanguage> builder)
    {
        builder.ToTable("CareerProfileLanguage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Level).HasMaxLength(80);
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerProfileCountryConfiguration : IEntityTypeConfiguration<CareerProfileCountry>
{
    public void Configure(EntityTypeBuilder<CareerProfileCountry> builder)
    {
        builder.ToTable("CareerProfileCountry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerProfileStackConfiguration : IEntityTypeConfiguration<CareerProfileStack>
{
    public void Configure(EntityTypeBuilder<CareerProfileStack> builder)
    {
        builder.ToTable("CareerProfileStack");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerProfileStrengthConfiguration : IEntityTypeConfiguration<CareerProfileStrength>
{
    public void Configure(EntityTypeBuilder<CareerProfileStrength> builder)
    {
        builder.ToTable("CareerProfileStrength");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Text).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerProfileEducationConfiguration : IEntityTypeConfiguration<CareerProfileEducation>
{
    public void Configure(EntityTypeBuilder<CareerProfileEducation> builder)
    {
        builder.ToTable("CareerProfileEducation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Place).HasMaxLength(200);
        builder.Property(x => x.Year).HasMaxLength(20);
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerProfileCertificationConfiguration : IEntityTypeConfiguration<CareerProfileCertification>
{
    public void Configure(EntityTypeBuilder<CareerProfileCertification> builder)
    {
        builder.ToTable("CareerProfileCertification");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Issuer).HasMaxLength(200);
        builder.Property(x => x.Year).HasMaxLength(20);
        builder.HasIndex(x => x.CareerProfileId);
    }
}

public class CareerCoverLetterConfiguration : IEntityTypeConfiguration<CareerCoverLetter>
{
    public void Configure(EntityTypeBuilder<CareerCoverLetter> builder)
    {
        builder.ToTable("CareerCoverLetter");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Stack).HasMaxLength(80);
        builder.Property(x => x.Body); // nvarchar(max)
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.CareerProfileId);
    }
}
