using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class KeyTypeConfiguration : IEntityTypeConfiguration<KeyTypeEntity>
{
    public void Configure(EntityTypeBuilder<KeyTypeEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("KeyTypes");
        builder.HasKey(entity => entity.TypeCode);
        builder.Property(entity => entity.TypeCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();
    }
}
