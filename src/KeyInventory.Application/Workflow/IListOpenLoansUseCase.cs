namespace KeyInventory.Application.Workflow;

public interface IListOpenLoansUseCase
{
    Task<IReadOnlyList<LoanListItem>> ExecuteAsync(CancellationToken cancellationToken);
}
