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
        string keyNumber,
        string medecoKeyCode,
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

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(keyNumber, medecoKeyCode, cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The selected physical key copy was not found.");
        }

        if (!keyAsset.IsActive)
        {
            throw new InvalidOperationException("An inactive physical key copy cannot be issued.");
        }

        if (await _loans.HasOpenLoanForKeyAssetAsync(keyAsset.KeyAssetId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("This physical key copy already has an open loan.");
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
            .FindDepartmentAsync(member.DepartmentId, cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department for the workforce member was not found.");
        }

        IReadOnlyList<WorkAssignment> activeAssignments = await _workforce
            .ListActiveWorkAssignmentsAsync(member.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        ArgumentException.ThrowIfNullOrWhiteSpace(justificationCode);

        KeyIssueEligibility.EnsureEligible(
            member,
            party,
            department,
            activeAssignments,
            kind,
            justificationCode);

        string normalizedJustification = justificationCode.Trim();
        Guid? justificationDepartmentId = null;
        string? justificationDepartmentCodeSnapshot = null;
        string? justificationRoomCode = null;
        if (kind == KeyIssueJustificationKind.Department)
        {
            justificationDepartmentId = department.DepartmentId;
            justificationDepartmentCodeSnapshot = department.DepartmentCode;
        }
        else
        {
            justificationRoomCode = normalizedJustification;
        }

        Loan loan = new(
            loanCode,
            keyAsset,
            party.PartyCode,
            issuedAtUtc,
            dueAtUtc,
            kind,
            justificationDepartmentId,
            justificationDepartmentCodeSnapshot,
            justificationRoomCode);

        string auditDetails =
            $"KEY#={keyAsset.KeyNumber}; MEDECO={keyAsset.MedecoKeyCode}; KeyAssetId={keyAsset.KeyAssetId:D}; WorkforceMember={member.WorkforceMemberCode}; Justification={kind}/{normalizedJustification}";
        if (kind == KeyIssueJustificationKind.Department)
        {
            auditDetails += $"; DepartmentId={department.DepartmentId:D}";
        }

        _audit.Stage(
            OperatorAuditActions.KeyIssued,
            OperatorAuditSubjects.Loan,
            loan.LoanCode,
            auditDetails);
        await _loans.AddLoanAsync(loan, cancellationToken).ConfigureAwait(false);
    }
}
