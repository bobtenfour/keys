using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Organizations");
        builder.HasKey(entity => entity.OrganizationCode);
        builder.Property(entity => entity.OrganizationCode).HasMaxLength(128);
    }
}
