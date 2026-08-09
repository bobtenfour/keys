using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyRoomAssignmentConfiguration : IEntityTypeConfiguration<KeyRoomAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<KeyRoomAssignmentEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("KeyRoomAssignments");
        builder.HasKey(entity => new { entity.CatalogKeyCode, entity.RoomCode });
        builder.Property(entity => entity.CatalogKeyCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RoomCode).HasMaxLength(128).IsRequired();
        builder.HasOne(entity => entity.KeyAsset)
            .WithMany()
            .HasForeignKey(entity => entity.CatalogKeyCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Room)
            .WithMany()
            .HasForeignKey(entity => entity.RoomCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.RoomCode);
    }
}
