using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class CareerApplicationStatusConfiguration : IEntityTypeConfiguration<CareerApplicationStatus>
{
    public void Configure(EntityTypeBuilder<CareerApplicationStatus> builder)
    {
        builder.ToTable("CareerApplicationStatus");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Color).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.SortOrder);
    }
}
