using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class OperatorAuditRecordConfiguration : IEntityTypeConfiguration<OperatorAuditRecordEntity>
{
    public void Configure(EntityTypeBuilder<OperatorAuditRecordEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("OperatorAuditRecords");
        builder.HasKey(entity => entity.AuditRecordId);
        builder.Property(entity => entity.AuditRecordId).HasMaxLength(64);
        builder.Property(entity => entity.OperatorReference).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.ActionType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.SubjectType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.SubjectReference).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Details).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.OccurredAtUtc).IsRequired();
        builder.HasIndex(entity => entity.OccurredAtUtc);
        builder.HasIndex(entity => entity.OperatorReference);
        builder.HasIndex(entity => entity.ActionType);
        builder.HasIndex(entity => entity.SubjectReference);
    }
}
