using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Domain.Loans;

/// <summary>
/// Loan aggregate — authoritative issuance intent and completion workflow only.
/// Does not own possession, custody, lifecycle, audit, Party profile, or catalog authority.
/// </summary>
public sealed class Loan
{
    public Loan(
        string loanCode,
        KeyAsset keyAsset,
        string borrowerPartyReference,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        KeyIssueJustificationKind justificationKind,
        Guid? justificationDepartmentId,
        string? justificationDepartmentCodeSnapshot,
        string? justificationRoomCode)
    {
        LoanCode = LoanText.Require(loanCode, nameof(loanCode));
        KeyAsset = keyAsset ?? throw new ArgumentNullException(nameof(keyAsset));
        BorrowerPartyReference = LoanText.Require(borrowerPartyReference, nameof(borrowerPartyReference));
        IssuedAtUtc = UtcTimestamp.Require(issuedAtUtc, nameof(issuedAtUtc));
        DueAtUtc = UtcTimestamp.Require(dueAtUtc, nameof(dueAtUtc));

        if (DueAtUtc <= IssuedAtUtc)
        {
            throw new ArgumentException("Due timestamp must be later than issue timestamp.", nameof(dueAtUtc));
        }

        ApplyJustification(
            justificationKind,
            justificationDepartmentId,
            justificationDepartmentCodeSnapshot,
            justificationRoomCode,
            allowLegacyUnset: false);

        Status = LoanStatus.Open;
    }

    private Loan(
        string loanCode,
        KeyAsset keyAsset,
        string borrowerPartyReference,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        LoanStatus status,
        KeyIssueJustificationKind justificationKind,
        Guid? justificationDepartmentId,
        string? justificationDepartmentCodeSnapshot,
        string? justificationRoomCode)
    {
        LoanCode = loanCode;
        KeyAsset = keyAsset;
        BorrowerPartyReference = borrowerPartyReference;
        IssuedAtUtc = issuedAtUtc;
        DueAtUtc = dueAtUtc;
        Status = status;
        JustificationKind = justificationKind;
        JustificationDepartmentId = justificationDepartmentId;
        JustificationDepartmentCodeSnapshot = justificationDepartmentCodeSnapshot;
        JustificationRoomCode = justificationRoomCode;
    }

    public string LoanCode { get; }

    public KeyAsset KeyAsset { get; }

    public string BorrowerPartyReference { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public DateTimeOffset DueAtUtc { get; }

    public KeyIssueJustificationKind JustificationKind { get; private set; }

    public Guid? JustificationDepartmentId { get; private set; }

    public string? JustificationDepartmentCodeSnapshot { get; private set; }

    public string? JustificationRoomCode { get; private set; }

    public LoanStatus Status { get; private set; }

    public bool IsOpenForReturn => Status == LoanStatus.Open;

    /// <summary>
    /// Reconstitutes a Loan from persistence. Allows Open or Returned status.
    /// Legacy rows may omit justification (all justification fields unset / Kind None).
    /// </summary>
    public static Loan Rehydrate(
        string loanCode,
        KeyAsset keyAsset,
        string borrowerPartyReference,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        LoanStatus status,
        KeyIssueJustificationKind justificationKind,
        Guid? justificationDepartmentId,
        string? justificationDepartmentCodeSnapshot,
        string? justificationRoomCode)
    {
        if (status is not (LoanStatus.Open or LoanStatus.Returned))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Only Open or Returned loans may be rehydrated.");
        }

        ArgumentNullException.ThrowIfNull(keyAsset);
        string code = LoanText.Require(loanCode, nameof(loanCode));
        string borrower = LoanText.Require(borrowerPartyReference, nameof(borrowerPartyReference));
        DateTimeOffset issued = UtcTimestamp.Require(issuedAtUtc, nameof(issuedAtUtc));
        DateTimeOffset due = UtcTimestamp.Require(dueAtUtc, nameof(dueAtUtc));
        if (due <= issued)
        {
            throw new ArgumentException("Due timestamp must be later than issue timestamp.", nameof(dueAtUtc));
        }

        Loan loan = new(
            code,
            keyAsset,
            borrower,
            issued,
            due,
            status,
            KeyIssueJustificationKind.None,
            null,
            null,
            null);
        loan.ApplyJustification(
            justificationKind,
            justificationDepartmentId,
            justificationDepartmentCodeSnapshot,
            justificationRoomCode,
            allowLegacyUnset: true);
        return loan;
    }

    public void Cancel()
    {
        if (Status != LoanStatus.Open)
        {
            throw new InvalidOperationException("Only an Open Loan may be cancelled.");
        }

        Status = LoanStatus.Cancelled;
    }

    internal void MarkReturned()
    {
        if (Status != LoanStatus.Open)
        {
            throw new InvalidOperationException("Only an Open Loan may be returned.");
        }

        Status = LoanStatus.Returned;
    }

    private void ApplyJustification(
        KeyIssueJustificationKind justificationKind,
        Guid? justificationDepartmentId,
        string? justificationDepartmentCodeSnapshot,
        string? justificationRoomCode,
        bool allowLegacyUnset)
    {
        bool hasAny =
            justificationKind != KeyIssueJustificationKind.None
            || justificationDepartmentId is not null
            || !string.IsNullOrWhiteSpace(justificationDepartmentCodeSnapshot)
            || !string.IsNullOrWhiteSpace(justificationRoomCode);

        if (!hasAny)
        {
            if (!allowLegacyUnset)
            {
                throw new ArgumentException("Loan justification is required.");
            }

            JustificationKind = KeyIssueJustificationKind.None;
            JustificationDepartmentId = null;
            JustificationDepartmentCodeSnapshot = null;
            JustificationRoomCode = null;
            return;
        }

        switch (justificationKind)
        {
            case KeyIssueJustificationKind.Department:
                if (justificationDepartmentId is null || justificationDepartmentId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "JustificationDepartmentId is required for Department justification.",
                        nameof(justificationDepartmentId));
                }

                string departmentSnapshot = LoanText.Require(
                    justificationDepartmentCodeSnapshot,
                    nameof(justificationDepartmentCodeSnapshot));
                if (!string.IsNullOrWhiteSpace(justificationRoomCode))
                {
                    throw new ArgumentException(
                        "JustificationRoomCode must be null for Department justification.",
                        nameof(justificationRoomCode));
                }

                JustificationKind = KeyIssueJustificationKind.Department;
                JustificationDepartmentId = justificationDepartmentId;
                JustificationDepartmentCodeSnapshot = departmentSnapshot;
                JustificationRoomCode = null;
                break;

            case KeyIssueJustificationKind.Room:
                string roomCode = LoanText.Require(justificationRoomCode, nameof(justificationRoomCode));
                if (justificationDepartmentId is not null)
                {
                    throw new ArgumentException(
                        "JustificationDepartmentId must be null for Room justification.",
                        nameof(justificationDepartmentId));
                }

                if (!string.IsNullOrWhiteSpace(justificationDepartmentCodeSnapshot))
                {
                    throw new ArgumentException(
                        "JustificationDepartmentCodeSnapshot must be null for Room justification.",
                        nameof(justificationDepartmentCodeSnapshot));
                }

                JustificationKind = KeyIssueJustificationKind.Room;
                JustificationDepartmentId = null;
                JustificationDepartmentCodeSnapshot = null;
                JustificationRoomCode = roomCode;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(justificationKind),
                    "Justification kind must be Department or Room.");
        }
    }
}
