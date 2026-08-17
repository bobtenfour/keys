using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure.Data;

namespace KeyInventory.Infrastructure.Workflow;

internal static class DomainLoanMapper
{
    internal static Loan ToOpenDomainLoan(LoanEntity entity)
    {
        if (!string.Equals(entity.Status, nameof(LoanStatus.Open), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only an open loan can be loaded for return completion.");
        }

        KeyAsset keyAsset = DomainCatalogMapper.ToDomain(entity.KeyAsset);
        return ToDomain(entity, keyAsset, LoanStatus.Open);
    }

    internal static Loan ToDomain(LoanEntity entity, KeyAsset keyAsset, LoanStatus status)
    {
        KeyIssueJustificationKind kind = ParseJustificationKind(entity.JustificationKind);
        return Loan.Rehydrate(
            entity.LoanCode,
            keyAsset,
            entity.BorrowerPartyReference,
            entity.IssuedAtUtc,
            entity.DueAtUtc,
            status,
            kind,
            entity.JustificationDepartmentId,
            entity.JustificationDepartmentCodeSnapshot,
            entity.JustificationRoomCode);
    }

    internal static LoanEntity ToEntity(Loan loan)
    {
        return new LoanEntity
        {
            LoanCode = loan.LoanCode,
            KeyAssetId = loan.KeyAsset.KeyAssetId,
            BorrowerPartyReference = loan.BorrowerPartyReference,
            IssuedAtUtc = loan.IssuedAtUtc,
            DueAtUtc = loan.DueAtUtc,
            Status = loan.Status.ToString(),
            JustificationKind = loan.JustificationKind == KeyIssueJustificationKind.None
                ? null
                : loan.JustificationKind.ToString(),
            JustificationDepartmentId = loan.JustificationDepartmentId,
            JustificationDepartmentCodeSnapshot = loan.JustificationDepartmentCodeSnapshot,
            JustificationRoomCode = loan.JustificationRoomCode
        };
    }

    internal static ReturnEntity ToEntity(Return completedReturn)
    {
        return new ReturnEntity
        {
            ReturnCode = completedReturn.ReturnCode,
            LoanCode = completedReturn.Loan.LoanCode,
            ReturnedAtUtc = completedReturn.ReturnedAtUtc
        };
    }

    private static KeyIssueJustificationKind ParseJustificationKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return KeyIssueJustificationKind.None;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out KeyIssueJustificationKind kind)
            || kind == KeyIssueJustificationKind.None)
        {
            throw new InvalidOperationException($"Unsupported loan justification kind '{value}'.");
        }

        return kind;
    }
}
