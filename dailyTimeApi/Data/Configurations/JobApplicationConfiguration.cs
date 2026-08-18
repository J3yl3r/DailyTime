using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplication");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.Property(x => x.Contact).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.JobApplications)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany(x => x.JobApplications)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.JobApplications)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Field)
            .WithMany(x => x.JobApplications)
            .HasForeignKey(x => x.FieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.JobApplications)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkExperience)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.WorkExperienceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.StatusId);
        builder.HasIndex(x => x.AppliedAt);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.PositionId);
    }
}
