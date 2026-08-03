using KeyInventory.Domain.Catalog;

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
        DateTimeOffset dueAtUtc)
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

        Status = LoanStatus.Open;
    }

    public string LoanCode { get; }

    public KeyAsset KeyAsset { get; }

    public string BorrowerPartyReference { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public DateTimeOffset DueAtUtc { get; }

    public LoanStatus Status { get; private set; }

    public bool IsOpenForReturn => Status == LoanStatus.Open;

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
}
