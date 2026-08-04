namespace KeyInventory.Application.Workflow;

public sealed class ListOpenLoansUseCase : IListOpenLoansUseCase
{
    private readonly ILoanPersistencePort _loans;

    public ListOpenLoansUseCase(ILoanPersistencePort loans)
    {
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
    }

    public Task<IReadOnlyList<LoanListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _loans.ListOpenLoansAsync(cancellationToken);
    }
}
