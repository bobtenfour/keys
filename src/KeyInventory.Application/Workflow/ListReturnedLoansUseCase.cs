namespace KeyInventory.Application.Workflow;

public sealed class ListReturnedLoansUseCase : IListReturnedLoansUseCase
{
    private readonly ILoanPersistencePort _loans;

    public ListReturnedLoansUseCase(ILoanPersistencePort loans)
    {
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
    }

    public Task<IReadOnlyList<LoanListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _loans.ListReturnedLoansAsync(cancellationToken);
    }
}
