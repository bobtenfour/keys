using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workflow;

public sealed class IssueLoanUseCase : IIssueLoanUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly ILoanPersistencePort _loans;
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public IssueLoanUseCase(
        IKeyCatalogPersistencePort catalog,
        ILoanPersistencePort loans,
        IWorkforcePersistencePort workforce,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string loanCode,
        string catalogKeyCode,
        string workforceMemberCode,
        string justificationKind,
        string justificationCode,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(justificationKind, ignoreCase: true, out KeyIssueJustificationKind kind)
            || kind == KeyIssueJustificationKind.None)
        {
            throw new InvalidOperationException("Justification must be Department or Room.");
        }

        if (await _loans.LoanExistsAsync(loanCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A loan with this loan code already exists.");
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(catalogKeyCode, cancellationToken).ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The selected key was not found.");
        }

        if (!keyAsset.IsActive)
        {
            throw new InvalidOperationException("An inactive key cannot be loaned.");
        }

        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        Party? party = await _workforce.FindPartyAsync(member.PartyCode, cancellationToken).ConfigureAwait(false);
        if (party is null)
        {
            throw new InvalidOperationException("The party for the workforce member was not found.");
        }

        Department? department = await _workforce
            .FindDepartmentAsync(member.DepartmentCode, cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department for the workforce member was not found.");
        }

        IReadOnlyList<WorkAssignment> activeAssignments = await _workforce
            .ListActiveWorkAssignmentsAsync(member.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        KeyIssueEligibility.EnsureEligible(
            member,
            party,
            department,
            activeAssignments,
            kind,
            justificationCode);

        Loan loan = new(loanCode, keyAsset, party.PartyCode, issuedAtUtc, dueAtUtc);
        _audit.Stage(
            OperatorAuditActions.KeyIssued,
            OperatorAuditSubjects.Loan,
            loan.LoanCode,
            $"Key={keyAsset.CatalogKeyCode}; WorkforceMember={member.WorkforceMemberCode}; Justification={kind}/{justificationCode?.Trim()}");
        await _loans.AddLoanAsync(loan, cancellationToken).ConfigureAwait(false);
    }
}
