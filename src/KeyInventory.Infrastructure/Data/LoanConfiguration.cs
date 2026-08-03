using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class LoanConfiguration : IEntityTypeConfiguration<LoanEntity>
{
    public void Configure(EntityTypeBuilder<LoanEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Loans");
        builder.HasKey(entity => entity.LoanCode);
        builder.Property(entity => entity.LoanCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.CatalogKeyCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.BorrowerPartyReference).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IssuedAtUtc).IsRequired();
        builder.Property(entity => entity.DueAtUtc).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(entity => entity.KeyAsset)
            .WithMany()
            .HasForeignKey(entity => entity.CatalogKeyCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
