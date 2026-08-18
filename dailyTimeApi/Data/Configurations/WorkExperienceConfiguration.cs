using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.ToTable("WorkExperience");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.Achievements).HasMaxLength(4000);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.WorkExperiences)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany(x => x.WorkExperiences)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.WorkExperiences)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Field)
            .WithMany(x => x.WorkExperiences)
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.PositionId);
        builder.HasIndex(x => x.StartDate);
    }
}
