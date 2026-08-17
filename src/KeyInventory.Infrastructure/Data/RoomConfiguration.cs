using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class RoomConfiguration : IEntityTypeConfiguration<RoomEntity>
{
    public void Configure(EntityTypeBuilder<RoomEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Rooms");
        builder.HasKey(entity => entity.RoomCode);
        builder.Property(entity => entity.RoomCode).HasMaxLength(128);
        builder.Property(entity => entity.RoomNumber).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.DepartmentId).IsRequired();
        builder.HasIndex(entity => entity.RoomNumber).IsUnique();
        builder.HasIndex(entity => entity.DepartmentId);
        builder.HasOne(entity => entity.Department)
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
