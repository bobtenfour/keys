using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAccessPatternConfiguration : IEntityTypeConfiguration<KeyAccessPatternEntity>
{
    public void Configure(EntityTypeBuilder<KeyAccessPatternEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("KeyAccessPatterns");
        builder.HasKey(entity => entity.KeyNumber);
        builder.Property(entity => entity.KeyNumber).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.KeyTypeCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.HasOne(entity => entity.KeyType)
            .WithMany()
            .HasForeignKey(entity => entity.KeyTypeCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
