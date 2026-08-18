using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class WorkItemStatusConfiguration : IEntityTypeConfiguration<WorkItemStatus>
{
    public void Configure(EntityTypeBuilder<WorkItemStatus> builder)
    {
        builder.ToTable("WorkItemStatus");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(7).IsRequired();
        builder.Property(x => x.ItemType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsFinal).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.ItemType);
    }
}
