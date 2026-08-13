using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class WorkAssignmentConfiguration : IEntityTypeConfiguration<WorkAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<WorkAssignmentEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("WorkAssignments");
        builder.HasKey(entity => entity.WorkAssignmentCode);
        builder.Property(entity => entity.WorkAssignmentCode).HasMaxLength(128);
        builder.Property(entity => entity.WorkforceMemberCode).HasMaxLength(128);
        builder.Property(entity => entity.RoomCode).HasMaxLength(128);
        builder.HasOne(entity => entity.WorkforceMember)
            .WithMany()
            .HasForeignKey(entity => entity.WorkforceMemberCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Room)
            .WithMany()
            .HasForeignKey(entity => entity.RoomCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.WorkforceMemberCode, entity.IsPrimary })
            .HasFilter("[IsActive] = 1 AND [IsPrimary] = 1")
            .IsUnique();
        builder.HasIndex(entity => entity.RoomCode);
    }
}
