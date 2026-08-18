using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class WorkItemCategoryConfiguration : IEntityTypeConfiguration<WorkItemCategory>
{
    public void Configure(EntityTypeBuilder<WorkItemCategory> builder)
    {
        builder.ToTable("WorkItemCategory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ItemType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => new { x.ItemType, x.Name });
    }
}
