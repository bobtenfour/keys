using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAssetConfiguration : IEntityTypeConfiguration<KeyAssetEntity>
{
    public void Configure(EntityTypeBuilder<KeyAssetEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("KeyAssets");
        builder.HasKey(entity => entity.CatalogKeyCode);
        builder.Property(entity => entity.CatalogKeyCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.KeyTypeCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.HasOne(entity => entity.KeyType)
            .WithMany()
            .HasForeignKey(entity => entity.KeyTypeCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
