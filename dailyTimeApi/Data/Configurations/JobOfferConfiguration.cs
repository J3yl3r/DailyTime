using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class JobOfferConfiguration : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder)
    {
        builder.ToTable("JobOffer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Company).HasMaxLength(200);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.Url).HasMaxLength(1000);
        builder.Property(x => x.ExternalKey).HasMaxLength(450).IsRequired();
        builder.Property(x => x.DescriptionSnippet).HasMaxLength(2000);
        builder.Property(x => x.Description); // nvarchar(max)
        builder.Property(x => x.Country).HasMaxLength(80);
        builder.Property(x => x.Language).HasMaxLength(20);
        builder.Property(x => x.WorkModality).HasMaxLength(80);
        builder.Property(x => x.ContractType).HasMaxLength(80);
        builder.Property(x => x.TechStack).HasMaxLength(200);
        builder.Property(x => x.PostedAt).HasPrecision(3);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.StatusSource).HasMaxLength(10).IsRequired();
        builder.Property(x => x.DiscardReason).HasMaxLength(500);
        builder.Property(x => x.PriorityTier).HasMaxLength(1);
        builder.Property(x => x.ScoreBreakdown); // nvarchar(max)
        builder.Property(x => x.ScoredAt).HasPrecision(3);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.CapturedAt).HasPrecision(3);
        builder.Property(x => x.UpdatedAt).HasPrecision(3);

        builder.HasIndex(x => new { x.JobPortalId, x.ExternalKey }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.PriorityScore);
        builder.HasIndex(x => x.IsPinned);
        builder.HasIndex(x => x.CapturedAt);
        builder.HasIndex(x => x.Country);
        builder.HasIndex(x => x.Language);
        builder.HasIndex(x => x.PostedAt);
        builder.HasIndex(x => x.WorkModality);
        builder.HasIndex(x => x.ContractType);
        builder.HasIndex(x => x.TechStack);

        builder.HasOne(x => x.JobPortal)
            .WithMany()
            .HasForeignKey(x => x.JobPortalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
