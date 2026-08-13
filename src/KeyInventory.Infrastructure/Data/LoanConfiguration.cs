using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeyInventory.Infrastructure.Data;

public sealed class LoanConfiguration : IEntityTypeConfiguration<LoanEntity>
{
    public void Configure(EntityTypeBuilder<LoanEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable(
            "Loans",
            table => table.HasCheckConstraint(
                "CK_Loans_Justification",
                """
                (
                    [JustificationKind] IS NULL
                    AND [JustificationDepartmentId] IS NULL
                    AND [JustificationDepartmentCodeSnapshot] IS NULL
                    AND [JustificationRoomCode] IS NULL
                )
                OR
                (
                    [JustificationKind] = N'Department'
                    AND [JustificationDepartmentId] IS NOT NULL
                    AND [JustificationDepartmentCodeSnapshot] IS NOT NULL
                    AND LTRIM(RTRIM([JustificationDepartmentCodeSnapshot])) <> N''
                    AND [JustificationRoomCode] IS NULL
                )
                OR
                (
                    [JustificationKind] = N'Room'
                    AND [JustificationRoomCode] IS NOT NULL
                    AND LTRIM(RTRIM([JustificationRoomCode])) <> N''
                    AND [JustificationDepartmentId] IS NULL
                    AND [JustificationDepartmentCodeSnapshot] IS NULL
                )
                """));
        builder.HasKey(entity => entity.LoanCode);
        builder.Property(entity => entity.LoanCode).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.KeyAssetId).IsRequired();
        builder.Property(entity => entity.BorrowerPartyReference).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IssuedAtUtc).IsRequired();
        builder.Property(entity => entity.DueAtUtc).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.JustificationKind).HasMaxLength(32);
        builder.Property(entity => entity.JustificationDepartmentCodeSnapshot).HasMaxLength(128);
        builder.Property(entity => entity.JustificationRoomCode).HasMaxLength(128);
        builder.HasOne(entity => entity.KeyAsset)
            .WithMany()
            .HasForeignKey(entity => entity.KeyAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.BorrowerParty)
            .WithMany()
            .HasForeignKey(entity => entity.BorrowerPartyReference)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.JustificationDepartment)
            .WithMany()
            .HasForeignKey(entity => entity.JustificationDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.JustificationRoom)
            .WithMany()
            .HasForeignKey(entity => entity.JustificationRoomCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.KeyAssetId);
        builder.HasIndex(entity => entity.BorrowerPartyReference);
        builder.HasIndex(entity => entity.JustificationDepartmentId);
        builder.HasIndex(entity => entity.JustificationRoomCode);
    }
}
