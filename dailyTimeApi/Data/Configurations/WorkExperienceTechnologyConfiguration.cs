using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class WorkExperienceTechnologyConfiguration : IEntityTypeConfiguration<WorkExperienceTechnology>
{
    public void Configure(EntityTypeBuilder<WorkExperienceTechnology> builder)
    {
        builder.ToTable("WorkExperienceTechnology");
        builder.HasKey(x => new { x.WorkExperienceId, x.TechnologyId });

        builder.HasOne(x => x.WorkExperience)
            .WithMany(x => x.Technologies)
            .HasForeignKey(x => x.WorkExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Technology)
            .WithMany(x => x.WorkExperiences)
            .HasForeignKey(x => x.TechnologyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TechnologyId);
    }
}
