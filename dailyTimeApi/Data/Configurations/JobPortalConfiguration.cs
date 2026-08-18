using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class JobPortalConfiguration : IEntityTypeConfiguration<JobPortal>
{
    public void Configure(EntityTypeBuilder<JobPortal> builder)
    {
        builder.ToTable("JobPortal");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LoginUrl).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ScrapeConfig).HasMaxLength(4000);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.LastRunStatus).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
