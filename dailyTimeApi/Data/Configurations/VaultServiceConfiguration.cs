using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class VaultServiceConfiguration : IEntityTypeConfiguration<VaultService>
{
    public void Configure(EntityTypeBuilder<VaultService> builder)
    {
        builder.ToTable("VaultService");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
