using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class BuildingConfiguration : IEntityTypeConfiguration<BuildingEntity>
{
    public void Configure(EntityTypeBuilder<BuildingEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Buildings");
        builder.HasKey(entity => entity.BuildingCode);
        builder.Property(entity => entity.BuildingCode).HasMaxLength(128);
    }
}
