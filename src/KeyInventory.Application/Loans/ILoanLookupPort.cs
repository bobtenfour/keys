using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Loans;

public interface ILoanLookupPort
{
    ValueTask<Loan?> FindByLoanCodeAsync(
        string loanCode,
        CancellationToken cancellationToken);
}
