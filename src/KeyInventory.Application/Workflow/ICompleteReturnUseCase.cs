namespace KeyInventory.Application.Workflow;

public interface ICompleteReturnUseCase
{
    Task ExecuteAsync(
        string returnCode,
        string loanCode,
        DateTimeOffset returnedAtUtc,
        CancellationToken cancellationToken);
}
