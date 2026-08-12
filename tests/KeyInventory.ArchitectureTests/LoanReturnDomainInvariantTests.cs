using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class LoanReturnDomainInvariantTests
{
    private static readonly DateTimeOffset IssueAt = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DueAt = IssueAt.AddDays(1);
    private static readonly DateTimeOffset ReturnAt = IssueAt.AddHours(5);

    [Fact]
    public void LoanRequiresLoanCode()
    {
        Assert.Throws<ArgumentException>(() => CreateLoan(loanCode: " "));
    }

    [Fact]
    public void LoanRequiresKeyAsset()
    {
        Assert.Throws<ArgumentNullException>(() => new Loan(
            "loan-1",
            keyAsset: null!,
            "party-1",
            IssueAt,
            DueAt));
    }

    [Fact]
    public void LoanRequiresBorrowerPartyReference()
    {
        Assert.Throws<ArgumentException>(() => CreateLoan(borrowerPartyReference: " "));
    }

    [Fact]
    public void LoanRequiresDueTimestampLaterThanIssueTimestamp()
    {
        Assert.Throws<ArgumentException>(() => CreateLoan(dueAtUtc: IssueAt));
        Assert.Throws<ArgumentException>(() => CreateLoan(dueAtUtc: IssueAt.AddTicks(-1)));
    }

    [Fact]
    public void LoanStartsOpen()
    {
        Loan loan = CreateLoan();

        Assert.Equal(LoanStatus.Open, loan.Status);
        Assert.True(loan.IsOpenForReturn);
    }

    [Fact]
    public void LoanCanBeCancelledOnlyWhileOpen()
    {
        Loan loan = CreateLoan();
        loan.Cancel();

        Assert.Equal(LoanStatus.Cancelled, loan.Status);
        Assert.False(loan.IsOpenForReturn);
        Assert.Throws<InvalidOperationException>(() => loan.Cancel());
    }

    [Fact]
    public void CancelledLoanCannotBeReturned()
    {
        Loan loan = CreateLoan();
        loan.Cancel();

        Assert.Throws<InvalidOperationException>(() => new Return("return-1", loan, ReturnAt));
        Assert.Equal(LoanStatus.Cancelled, loan.Status);
    }

    [Fact]
    public void ReturnRequiresReturnCode()
    {
        Loan loan = CreateLoan();

        Assert.Throws<ArgumentException>(() => new Return(" ", loan, ReturnAt));
    }

    [Fact]
    public void ReturnRequiresOpenLoan()
    {
        Loan loan = CreateLoan();
        _ = new Return("return-1", loan, ReturnAt);

        Assert.Throws<InvalidOperationException>(() => new Return("return-2", loan, ReturnAt.AddMinutes(1)));
    }

    [Fact]
    public void ReturnTimestampMustNotBeEarlierThanLoanIssueTimestamp()
    {
        Loan loan = CreateLoan();

        Assert.Throws<ArgumentException>(() => new Return("return-1", loan, IssueAt.AddTicks(-1)));
    }

    [Fact]
    public void ReturnTimestampMayEqualLoanIssueTimestamp()
    {
        Loan loan = CreateLoan();
        Return completed = new("return-1", loan, IssueAt);

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(IssueAt, completed.ReturnedAtUtc);
    }

    [Fact]
    public void ExactlyOneReturnMayCompleteALoan()
    {
        Loan loan = CreateLoan();
        Return first = new("return-1", loan, ReturnAt);

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Same(loan, first.Loan);
        Assert.Throws<InvalidOperationException>(() => new Return("return-2", loan, ReturnAt.AddMinutes(1)));
    }

    private static Loan CreateLoan(
        string loanCode = "loan-1",
        string borrowerPartyReference = "party-1",
        DateTimeOffset? dueAtUtc = null)
    {
        KeyAsset keyAsset = CatalogTestFactory.CreateCopy("key-1", "01", "mechanical");
        return new Loan(loanCode, keyAsset, borrowerPartyReference, IssueAt, dueAtUtc ?? DueAt);
    }
}
