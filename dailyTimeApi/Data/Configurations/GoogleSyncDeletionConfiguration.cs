using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class GoogleSyncDeletionConfiguration : IEntityTypeConfiguration<GoogleSyncDeletion>
{
    public void Configure(EntityTypeBuilder<GoogleSyncDeletion> builder)
    {
        builder.ToTable("GoogleSyncDeletion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GoogleEventId).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CalendarId).HasMaxLength(320).IsRequired();
        builder.Property(x => x.DeletedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.Attempts).HasDefaultValue(0);
        builder.Property(x => x.LastError).HasMaxLength(500);
        builder.HasIndex(x => x.GoogleEventId);
    }
}
