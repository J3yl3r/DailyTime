using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("TimeEntry");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasCheckConstraint("CK_TimeEntry_DurationMinutes", "[DurationMinutes] > 0");
        builder.HasCheckConstraint(
            "CK_TimeEntry_OneOwner",
            "([TaskItemId] IS NOT NULL AND [NoteId] IS NULL) OR ([TaskItemId] IS NULL AND [NoteId] IS NOT NULL)");

        builder.HasOne(x => x.TaskItem)
            .WithMany(x => x.TimeEntries)
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Note)
            .WithMany(x => x.TimeEntries)
            .HasForeignKey(x => x.NoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TaskItemId)
            .HasFilter("[TaskItemId] IS NOT NULL");

        builder.HasIndex(x => x.NoteId)
            .HasFilter("[NoteId] IS NOT NULL");

        builder.HasIndex(x => x.WorkDate);
    }
}