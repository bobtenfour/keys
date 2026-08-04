using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
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
        return new Loan(
            entity.LoanCode,
            keyAsset,
            entity.BorrowerPartyReference,
            entity.IssuedAtUtc,
            entity.DueAtUtc);
    }

    internal static LoanEntity ToEntity(Loan loan)
    {
        return new LoanEntity
        {
            LoanCode = loan.LoanCode,
            CatalogKeyCode = loan.KeyAsset.CatalogKeyCode,
            BorrowerPartyReference = loan.BorrowerPartyReference,
            IssuedAtUtc = loan.IssuedAtUtc,
            DueAtUtc = loan.DueAtUtc,
            Status = loan.Status.ToString()
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
}
