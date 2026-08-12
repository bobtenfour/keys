using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Workflow;

public sealed class CompleteReturnUseCase : ICompleteReturnUseCase
{
    private readonly ILoanPersistencePort _loans;
    private readonly IOperatorAuditRecorder _audit;

    public CompleteReturnUseCase(ILoanPersistencePort loans, IOperatorAuditRecorder audit)
    {
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
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
        _audit.Stage(
            OperatorAuditActions.KeyReturned,
            OperatorAuditSubjects.Return,
            completedReturn.ReturnCode,
            $"Loan={loan.LoanCode}; KEY#={loan.KeyAsset.KeyNumber}; MEDECO={loan.KeyAsset.MedecoKeyCode}; KeyAssetId={loan.KeyAsset.KeyAssetId:D}");
        await _loans.AddReturnAsync(completedReturn, cancellationToken).ConfigureAwait(false);
    }
}
