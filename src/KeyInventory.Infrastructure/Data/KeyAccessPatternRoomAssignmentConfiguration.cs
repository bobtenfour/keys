using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAccessPatternRoomAssignmentConfiguration
    : IEntityTypeConfiguration<KeyAccessPatternRoomAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<KeyAccessPatternRoomAssignmentEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("KeyAccessPatternRoomAssignments");
        builder.HasKey(entity => new { entity.KeyNumber, entity.RoomCode });
        builder.Property(entity => entity.KeyNumber).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RoomCode).HasMaxLength(128).IsRequired();
        builder.HasOne(entity => entity.KeyAccessPattern)
            .WithMany()
            .HasForeignKey(entity => entity.KeyNumber)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Room)
            .WithMany()
            .HasForeignKey(entity => entity.RoomCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.RoomCode);
    }
}
