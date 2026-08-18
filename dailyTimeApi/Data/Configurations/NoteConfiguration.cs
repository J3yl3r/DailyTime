using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Note");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.DurationMinutes).HasDefaultValue(0);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.StartTime).HasColumnType("time(0)");
        builder.Property(x => x.EndTime).HasColumnType("time(0)");

        builder.HasCheckConstraint("CK_Note_DurationMinutes", "[DurationMinutes] >= 0");
        builder.HasCheckConstraint(
            "CK_Note_Schedule",
            "([StartTime] IS NULL AND [EndTime] IS NULL) OR " +
            "([WorkDate] IS NOT NULL AND [StartTime] IS NOT NULL AND " +
            "[EndTime] IS NOT NULL AND [EndTime] > [StartTime])");

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Notes)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Notes)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Person)
            .WithMany(x => x.Notes)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Project)
            .WithMany(x => x.Notes)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company)
            .WithMany(x => x.Notes)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentNoteId);
        builder.HasIndex(x => x.WorkDate);
        builder.HasIndex(x => x.StatusId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.PersonId);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => new { x.WorkDate, x.StatusId });
        builder.HasIndex(x => new { x.WorkDate, x.StartTime });
    }
}