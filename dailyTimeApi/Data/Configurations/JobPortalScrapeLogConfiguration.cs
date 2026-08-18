using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class JobPortalScrapeLogConfiguration : IEntityTypeConfiguration<JobPortalScrapeLog>
{
    public void Configure(EntityTypeBuilder<JobPortalScrapeLog> builder)
    {
        builder.ToTable("JobPortalScrapeLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000);
        builder.Property(x => x.StartedAt).HasPrecision(3).IsRequired();
        builder.Property(x => x.FinishedAt).HasPrecision(3);
        builder.Property(x => x.OfferCount).HasDefaultValue(0);
        builder.Property(x => x.SavedInserted).HasDefaultValue(0);
        builder.Property(x => x.SavedUpdated).HasDefaultValue(0);

        builder.HasIndex(x => new { x.JobPortalId, x.StartedAt });
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.JobPortal)
            .WithMany()
            .HasForeignKey(x => x.JobPortalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
