using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface IListOutstandingReturnObligationsUseCase
{
    Task<IReadOnlyList<OutstandingReturnObligationItem>> ExecuteAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);
}

/// <summary>
/// Exposes mandatory outstanding return obligations for a Terminated WorkforceMember.
/// Reads Open Loans only; does not mutate Loan, Return, Audit, Custody, or Lifecycle.
/// </summary>
public sealed class ListOutstandingReturnObligationsUseCase : IListOutstandingReturnObligationsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly ILoanPersistencePort _loans;

    public ListOutstandingReturnObligationsUseCase(
        IWorkforcePersistencePort workforce,
        ILoanPersistencePort loans)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
    }

    public async Task<IReadOnlyList<OutstandingReturnObligationItem>> ExecuteAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        if (member.Status != WorkforceMemberStatus.Terminated)
        {
            throw new InvalidOperationException(
                "Outstanding return obligations are exposed for Terminated WorkforceMember records.");
        }

        IReadOnlyList<LoanListItem> openLoans = await _loans.ListOpenLoansAsync(cancellationToken).ConfigureAwait(false);
        return openLoans
            .Where(loan => string.Equals(loan.BorrowerPartyReference, member.PartyCode, StringComparison.Ordinal))
            .Select(loan => new OutstandingReturnObligationItem(
                loan.LoanCode,
                loan.KeyNumber,
                loan.MedecoKeyCode,
                loan.BorrowerPartyReference,
                loan.IssuedAtUtc,
                loan.DueAtUtc))
            .ToArray();
    }
}
