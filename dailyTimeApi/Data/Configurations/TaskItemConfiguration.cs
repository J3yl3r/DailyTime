using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("TaskItem");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Content);
            builder.Property(x => x.IsCompleted).HasDefaultValue(false);
            builder.Property(x => x.DurationMinutes).HasDefaultValue(0);
            builder.Property(x => x.SortOrder).HasDefaultValue(0);
            builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(x => x.CompletedAt).HasPrecision(3);
            builder.Property(x => x.StartTime).HasColumnType("time(0)");
            builder.Property(x => x.EndTime).HasColumnType("time(0)");
            builder.HasCheckConstraint("CK_TaskItem_DurationMinutes", "[DurationMinutes] >= 0");
            builder.HasCheckConstraint(
                "CK_TaskItem_Schedule",
                "([StartTime] IS NULL AND [EndTime] IS NULL) OR " +
                "([StartTime] IS NOT NULL AND [EndTime] IS NOT NULL AND [EndTime] > [StartTime])");
            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Status)
                .WithMany(x => x.TaskItems)
                .HasForeignKey(x => x.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Category)
                .WithMany(x => x.TaskItems)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Person)
                .WithMany(x => x.TaskItems)
                .HasForeignKey(x => x.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Project)
                .WithMany(x => x.TaskItems)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Company)
                .WithMany(x => x.TaskItems)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => x.ParentTaskId);
            builder.HasIndex(x => x.WorkDate);
            builder.HasIndex(x => x.StatusId);
            builder.HasIndex(x => x.CategoryId);
            builder.HasIndex(x => x.PersonId);
            builder.HasIndex(x => x.ProjectId);
            builder.HasIndex(x => x.CompanyId);
            builder.HasIndex(x => new { x.WorkDate, x.SortOrder });
            builder.HasIndex(x => new { x.WorkDate, x.StatusId });
            builder.HasIndex(x => new { x.WorkDate, x.StartTime });

            // Correlación con Google Calendar (evento espejo del elemento).
            builder.Property(x => x.GoogleEventId).HasMaxLength(300);
            builder.Property(x => x.GoogleEtag).HasMaxLength(100);
            builder.Property(x => x.GoogleSyncedAt).HasPrecision(3);
            builder.Property(x => x.GoogleUpdatedAt).HasPrecision(3);
            builder.Property(x => x.SyncSource).HasMaxLength(20);
            builder.Property(x => x.GoogleColor).HasMaxLength(9);
            builder.HasIndex(x => x.GoogleEventId)
                .IsUnique()
                .HasFilter("[GoogleEventId] IS NOT NULL");
        }
    }
}
