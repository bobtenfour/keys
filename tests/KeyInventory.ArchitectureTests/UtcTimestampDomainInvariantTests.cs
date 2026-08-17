using KeyInventory.Domain;
using KeyInventory.Domain.Audit;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Identity;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class UtcTimestampDomainInvariantTests
{
    private static readonly DateTimeOffset UtcInstant = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NonUtcInstant = new(2026, 7, 30, 12, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void UtcTimestampRequireAcceptsZeroOffsetTimestamp()
    {
        DateTimeOffset validated = UtcTimestamp.Require(UtcInstant, "value");

        Assert.Equal(UtcInstant, validated);
        Assert.Equal(TimeSpan.Zero, validated.Offset);
    }

    [Fact]
    public void UtcTimestampRequireRejectsNonZeroOffset()
    {
        Assert.Throws<ArgumentException>(() => UtcTimestamp.Require(NonUtcInstant, "value"));
    }

    [Fact]
    public void UtcTimestampRequireRejectsDefaultTimestamp()
    {
        Assert.Throws<ArgumentException>(() => UtcTimestamp.Require(default, "value"));
    }

    [Fact]
    public void UtcTimestampRequireDoesNotNormalizeNonUtcValues()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => UtcTimestamp.Require(NonUtcInstant, "value"));

        Assert.Equal("value", exception.ParamName);
        Assert.NotEqual(TimeSpan.Zero, NonUtcInstant.Offset);
    }

    [Fact]
    public void LoanRejectsNonUtcIssuedAtUtc()
    {
        Assert.Throws<ArgumentException>(() => CreateLoan(issuedAtUtc: NonUtcInstant, dueAtUtc: UtcInstant.AddDays(1)));
    }

    [Fact]
    public void LoanRejectsNonUtcDueAtUtc()
    {
        Assert.Throws<ArgumentException>(() => CreateLoan(issuedAtUtc: UtcInstant, dueAtUtc: NonUtcInstant.AddDays(1)));
    }

    [Fact]
    public void LoanRejectsDefaultIssuedAtUtc()
    {
        Assert.Throws<ArgumentException>(() => CreateLoan(issuedAtUtc: default, dueAtUtc: UtcInstant));
    }

    [Fact]
    public void LoanAcceptsUtcTimestamps()
    {
        Loan loan = CreateLoan(issuedAtUtc: UtcInstant, dueAtUtc: UtcInstant.AddDays(1));

        Assert.Equal(UtcInstant, loan.IssuedAtUtc);
        Assert.Equal(UtcInstant.AddDays(1), loan.DueAtUtc);
    }

    [Fact]
    public void ReturnRejectsNonUtcReturnedAtUtc()
    {
        Loan loan = CreateLoan(issuedAtUtc: UtcInstant, dueAtUtc: UtcInstant.AddDays(1));

        Assert.Throws<ArgumentException>(() => new Return("return-1", loan, NonUtcInstant));
    }

    [Fact]
    public void ReturnAcceptsUtcReturnedAtUtc()
    {
        Loan loan = CreateLoan(issuedAtUtc: UtcInstant, dueAtUtc: UtcInstant.AddDays(1));
        Return completed = new("return-1", loan, UtcInstant);

        Assert.Equal(UtcInstant, completed.ReturnedAtUtc);
    }

    [Fact]
    public void AuditEventRejectsNonUtcOccurredAtUtc()
    {
        Assert.Throws<ArgumentException>(() => new AuditEvent(
            "audit-1",
            "LoanIssued",
            NonUtcInstant,
            CreatePrincipal()));
    }

    [Fact]
    public void AuditEventAcceptsUtcOccurredAtUtc()
    {
        AuditEvent auditEvent = new("audit-1", "LoanIssued", UtcInstant, CreatePrincipal());

        Assert.Equal(UtcInstant, auditEvent.OccurredAtUtc);
    }

    [Fact]
    public void PrincipalRoleAssignmentRejectsNonUtcEffectiveFromUtc()
    {
        Assert.Throws<ArgumentException>(() => CreateAssignment(
            effectiveFromUtc: NonUtcInstant,
            effectiveToUtc: null));
    }

    [Fact]
    public void PrincipalRoleAssignmentRejectsNonUtcEffectiveToUtc()
    {
        Assert.Throws<ArgumentException>(() => CreateAssignment(
            effectiveFromUtc: UtcInstant,
            effectiveToUtc: NonUtcInstant.AddDays(1)));
    }

    [Fact]
    public void PrincipalRoleAssignmentAcceptsUtcEffectiveWindow()
    {
        PrincipalRoleAssignment assignment = CreateAssignment(
            effectiveFromUtc: UtcInstant,
            effectiveToUtc: UtcInstant.AddDays(1));

        Assert.Equal(UtcInstant, assignment.EffectiveFromUtc);
        Assert.Equal(UtcInstant.AddDays(1), assignment.EffectiveToUtc);
    }

    private static Loan CreateLoan(DateTimeOffset issuedAtUtc, DateTimeOffset dueAtUtc)
    {
        KeyAsset keyAsset = CatalogTestFactory.CreateCopy("key-1", "01", KeyAccessClassification.Regular);
        Guid departmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        return new Loan(
            "loan-1",
            keyAsset,
            "party-1",
            issuedAtUtc,
            dueAtUtc,
            KeyIssueJustificationKind.Department,
            departmentId,
            "DEPT",
            null);
    }

    private static PrincipalRoleAssignment CreateAssignment(
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc)
    {
        SecurityPrincipal principal = CreatePrincipal();
        Role role = new("security-admin");
        return new PrincipalRoleAssignment(
            principal,
            role,
            AuthorizationScopeType.Global,
            "global",
            effectiveFromUtc,
            effectiveToUtc);
    }

    private static SecurityPrincipal CreatePrincipal()
    {
        return new SecurityPrincipal("auditor-1", SecurityPrincipalType.System, partyReference: null);
    }
}
