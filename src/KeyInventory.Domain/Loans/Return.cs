using System.Diagnostics.CodeAnalysis;

namespace KeyInventory.Domain.Loans;

/// <summary>
/// Return aggregate — authoritative completion of one Open Loan.
/// Does not own possession, custody, lifecycle, audit, Party profile, or catalog authority.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Return is the contract-required loan/return aggregate root name in key-inventory-domain-contract.md.")]
public sealed class Return
{
    public Return(string returnCode, Loan loan, DateTimeOffset returnedAtUtc)
    {
        ReturnCode = LoanText.Require(returnCode, nameof(returnCode));
        ArgumentNullException.ThrowIfNull(loan);

        if (!loan.IsOpenForReturn)
        {
            throw new InvalidOperationException("Return requires an Open Loan.");
        }

        if (returnedAtUtc < loan.IssuedAtUtc)
        {
            throw new ArgumentException(
                "Return timestamp must not be earlier than the Loan issue timestamp.",
                nameof(returnedAtUtc));
        }

        Loan = loan;
        ReturnedAtUtc = returnedAtUtc;
        loan.MarkReturned();
    }

    public string ReturnCode { get; }

    public Loan Loan { get; }

    public DateTimeOffset ReturnedAtUtc { get; }
}
