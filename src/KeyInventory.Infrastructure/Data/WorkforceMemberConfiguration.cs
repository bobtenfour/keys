using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class WorkforceMemberConfiguration : IEntityTypeConfiguration<WorkforceMemberEntity>
{
    public void Configure(EntityTypeBuilder<WorkforceMemberEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("WorkforceMembers");
        builder.HasKey(entity => entity.WorkforceMemberCode);
        builder.Property(entity => entity.WorkforceMemberCode).HasMaxLength(128);
        builder.Property(entity => entity.PartyCode).HasMaxLength(128);
        builder.Property(entity => entity.WorkforceType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DepartmentCode).HasMaxLength(128);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(entity => entity.Party)
            .WithMany()
            .HasForeignKey(entity => entity.PartyCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PartyCode)
            .HasFilter("[Status] = 'Active'")
            .IsUnique();
    }
}
