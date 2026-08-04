namespace KeyInventory.Application.Workflow;

public interface IListReturnedLoansUseCase
{
    Task<IReadOnlyList<LoanListItem>> ExecuteAsync(CancellationToken cancellationToken);
}
