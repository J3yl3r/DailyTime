using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class OfferTriageConfigConfiguration : IEntityTypeConfiguration<OfferTriageConfig>
{
    public void Configure(EntityTypeBuilder<OfferTriageConfig> builder)
    {
        builder.ToTable("OfferTriageConfig");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SettingsJson).IsRequired(); // nvarchar(max)
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
