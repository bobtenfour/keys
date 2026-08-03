using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class ReturnConfiguration : IEntityTypeConfiguration<ReturnEntity>
{
    public void Configure(EntityTypeBuilder<ReturnEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Returns");
        builder.HasKey(entity => entity.ReturnCode);
        builder.Property(entity => entity.ReturnCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.LoanCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.ReturnedAtUtc).IsRequired();
        builder.HasOne(entity => entity.Loan)
            .WithMany()
            .HasForeignKey(entity => entity.LoanCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.LoanCode).IsUnique();
    }
}
