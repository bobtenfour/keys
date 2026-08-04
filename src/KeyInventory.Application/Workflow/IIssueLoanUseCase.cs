namespace KeyInventory.Application.Workflow;

public interface IIssueLoanUseCase
{
    Task ExecuteAsync(
        string loanCode,
        string catalogKeyCode,
        string borrowerPartyReference,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        CancellationToken cancellationToken);
}
