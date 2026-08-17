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
        builder.Property(entity => entity.Classification).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.RoomCode).HasMaxLength(128);
        builder.Property(entity => entity.IsActive).IsRequired();
        builder.HasIndex(entity => entity.RoomCode);
        builder.HasOne(entity => entity.Room)
            .WithMany()
            .HasForeignKey(entity => entity.RoomCode)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
