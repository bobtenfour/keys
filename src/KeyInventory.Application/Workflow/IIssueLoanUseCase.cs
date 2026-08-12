namespace KeyInventory.Application.Workflow;

public interface IIssueLoanUseCase
{
    Task ExecuteAsync(
        string loanCode,
        string keyNumber,
        string medecoKeyCode,
        string workforceMemberCode,
        string justificationKind,
        string justificationCode,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        CancellationToken cancellationToken);
}
