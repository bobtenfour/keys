using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Workflow;

public sealed class CompleteReturnUseCase : ICompleteReturnUseCase
{
    private readonly ILoanPersistencePort _loans;

    public CompleteReturnUseCase(ILoanPersistencePort loans)
    {
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
    }

    public async Task ExecuteAsync(
        string returnCode,
        string loanCode,
        DateTimeOffset returnedAtUtc,
        CancellationToken cancellationToken)
    {
        Loan? loan = await _loans.FindOpenLoanAsync(loanCode, cancellationToken).ConfigureAwait(false);
        if (loan is null)
        {
            throw new InvalidOperationException("An open loan with this loan code was not found.");
        }

        Return completedReturn = new(returnCode, loan, returnedAtUtc);
        await _loans.AddReturnAsync(completedReturn, cancellationToken).ConfigureAwait(false);
    }
}
