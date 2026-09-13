using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class ScrapeScheduleConfiguration : IEntityTypeConfiguration<ScrapeSchedule>
{
    public void Configure(EntityTypeBuilder<ScrapeSchedule> builder)
    {
        builder.ToTable("ScrapeSchedule");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Times).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Days).HasMaxLength(20);
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ConfigUpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.LastSlotAt).HasPrecision(3);
        builder.Property(x => x.LastSlotStatus).HasMaxLength(20);
        builder.Property(x => x.LastSlotMessage).HasMaxLength(1000);
        builder.Property(x => x.LastSlotFinishedAt).HasPrecision(3);
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
