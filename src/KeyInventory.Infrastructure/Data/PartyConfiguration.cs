using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class PartyConfiguration : IEntityTypeConfiguration<PartyEntity>
{
    public void Configure(EntityTypeBuilder<PartyEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Parties");
        builder.HasKey(entity => entity.PartyCode);
        builder.Property(entity => entity.PartyCode).HasMaxLength(128);
        builder.Property(entity => entity.FirstName).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.LastName).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Uin).HasMaxLength(9).IsRequired();
        builder.HasIndex(entity => entity.Uin).IsUnique();
    }
}
