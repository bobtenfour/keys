using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Departments");
        builder.HasKey(entity => entity.DepartmentId);
        builder.Property(entity => entity.DepartmentId).ValueGeneratedNever();
        builder.Property(entity => entity.DepartmentCode).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => entity.DepartmentCode).IsUnique();
    }
}
