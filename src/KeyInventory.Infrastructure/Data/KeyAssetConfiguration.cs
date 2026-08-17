using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAssetConfiguration : IEntityTypeConfiguration<KeyAssetEntity>
{
    public void Configure(EntityTypeBuilder<KeyAssetEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("KeyAssets");
        builder.HasKey(entity => entity.KeyAssetId);
        builder.Property(entity => entity.KeyAssetId).ValueGeneratedNever();
        builder.Property(entity => entity.KeyNumber).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.MedecoKeyCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Condition).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.KeyNumber, entity.MedecoKeyCode }).IsUnique();
        builder.HasIndex(entity => entity.Condition);
        builder.HasIndex(entity => entity.ReplacesKeyAssetId);
        builder.HasOne(entity => entity.AccessPattern)
            .WithMany()
            .HasForeignKey(entity => entity.KeyNumber)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ReplacesKeyAsset)
            .WithMany()
            .HasForeignKey(entity => entity.ReplacesKeyAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
