using dailyTimeApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace dailyTimeApi.Data.Configurations;

public class VaultPasswordConfiguration : IEntityTypeConfiguration<VaultPassword>
{
    public void Configure(EntityTypeBuilder<VaultPassword> builder)
    {
        builder.ToTable("VaultPassword");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Password).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Tags).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Account)
            .WithMany(x => x.Passwords)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Service)
            .WithMany(x => x.Passwords)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => x.ServiceId);
    }
}
