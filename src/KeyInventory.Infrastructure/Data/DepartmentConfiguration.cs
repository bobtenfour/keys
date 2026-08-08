using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Departments");
        builder.HasKey(entity => new { entity.OrganizationCode, entity.DepartmentCode });
        builder.Property(entity => entity.OrganizationCode).HasMaxLength(128);
        builder.Property(entity => entity.DepartmentCode).HasMaxLength(128);
        builder.HasOne(entity => entity.Organization)
            .WithMany()
            .HasForeignKey(entity => entity.OrganizationCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
