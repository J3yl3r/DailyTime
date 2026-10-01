using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class GoogleCalendarAccountConfiguration : IEntityTypeConfiguration<GoogleCalendarAccount>
{
    public void Configure(EntityTypeBuilder<GoogleCalendarAccount> builder)
    {
        builder.ToTable("GoogleCalendarAccount");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.AccessToken).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.RefreshToken).HasMaxLength(512).IsRequired();
        builder.Property(x => x.AccessTokenExpiresAt).HasPrecision(3);
        builder.Property(x => x.CalendarId).HasMaxLength(320).IsRequired();
        builder.Property(x => x.PullCutoffAt).HasPrecision(3);
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SyncEnabled).HasDefaultValue(true);
        builder.Property(x => x.SyncTimedTasks).HasDefaultValue(true);
        builder.Property(x => x.SyncAllDayTasks).HasDefaultValue(true);
        builder.Property(x => x.SyncTimedNotes).HasDefaultValue(true);
        builder.Property(x => x.PastDays).HasDefaultValue(30);
        builder.Property(x => x.FutureDays).HasDefaultValue(180);
        builder.Property(x => x.LastSyncAt).HasPrecision(3);
        builder.Property(x => x.LastSyncStatus).HasMaxLength(20);
        builder.Property(x => x.LastSyncMessage).HasMaxLength(1000);
        builder.Property(x => x.ConnectedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasCheckConstraint("CK_GoogleCalendarAccount_Window", "[PastDays] >= 0 AND [FutureDays] >= 0");
    }
}
