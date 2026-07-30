using System.Reflection;
using KeyInventory.Domain.Audit;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Identity;
using KeyInventory.Domain.Loans;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class AuditDomainInvariantTests
{
    private static readonly DateTimeOffset OccurredAt = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AuditEventRequiresAuditEventCode()
    {
        Assert.Throws<ArgumentException>(() => CreateAuditEvent(auditEventCode: " "));
    }

    [Fact]
    public void AuditEventRequiresActionType()
    {
        Assert.Throws<ArgumentException>(() => CreateAuditEvent(actionType: " "));
    }

    [Fact]
    public void AuditEventRequiresActingSecurityPrincipal()
    {
        Assert.Throws<ArgumentNullException>(() => new AuditEvent(
            "audit-1",
            "LoanIssued",
            OccurredAt,
            actingSecurityPrincipal: null!));
    }

    [Fact]
    public void AuditEventStoresRequiredEvidenceFields()
    {
        SecurityPrincipal principal = CreatePrincipal();
        AuditEvent auditEvent = CreateAuditEvent(actingSecurityPrincipal: principal);

        Assert.Equal("audit-1", auditEvent.AuditEventCode);
        Assert.Equal("LoanIssued", auditEvent.ActionType);
        Assert.Equal(OccurredAt, auditEvent.OccurredAtUtc);
        Assert.Same(principal, auditEvent.ActingSecurityPrincipal);
        Assert.Null(auditEvent.PartyReference);
        Assert.Null(auditEvent.SubjectKeyAsset);
        Assert.Null(auditEvent.SubjectLoan);
        Assert.Null(auditEvent.SubjectReturn);
    }

    [Fact]
    public void AuditEventIsImmutableAfterCreation()
    {
        Type type = typeof(AuditEvent);

        string[] publicSetters = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.SetMethod is not null && property.SetMethod.IsPublic)
            .Select(property => property.Name)
            .ToArray();

        string[] mutatingMethods = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(publicSetters);
        Assert.Empty(mutatingMethods);
    }

    [Fact]
    public void AuditEventMayOptionallyReferencePartyKeyAssetLoanOrReturnWithoutMutatingThem()
    {
        KeyType keyType = new("mechanical");
        KeyAsset keyAsset = new("key-1", keyType);
        Loan loan = new(
            "loan-1",
            keyAsset,
            "party-1",
            OccurredAt,
            OccurredAt.AddDays(1));
        Return completedReturn = new("return-1", loan, OccurredAt.AddHours(1));
        LoanStatus statusAfterReturn = loan.Status;
        string catalogKeyCode = keyAsset.CatalogKeyCode;
        string returnCode = completedReturn.ReturnCode;

        AuditEvent auditEvent = new(
            "audit-1",
            "LoanReturned",
            OccurredAt.AddHours(1),
            CreatePrincipal(),
            partyReference: "party-1",
            subjectKeyAsset: keyAsset,
            subjectLoan: loan,
            subjectReturn: completedReturn);

        Assert.Equal("party-1", auditEvent.PartyReference);
        Assert.Same(keyAsset, auditEvent.SubjectKeyAsset);
        Assert.Same(loan, auditEvent.SubjectLoan);
        Assert.Same(completedReturn, auditEvent.SubjectReturn);
        Assert.Equal(LoanStatus.Returned, statusAfterReturn);
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(catalogKeyCode, keyAsset.CatalogKeyCode);
        Assert.Equal(returnCode, completedReturn.ReturnCode);
    }

    private static AuditEvent CreateAuditEvent(
        string auditEventCode = "audit-1",
        string actionType = "LoanIssued",
        SecurityPrincipal? actingSecurityPrincipal = null)
    {
        return new AuditEvent(
            auditEventCode,
            actionType,
            OccurredAt,
            actingSecurityPrincipal ?? CreatePrincipal());
    }

    private static SecurityPrincipal CreatePrincipal()
    {
        return new SecurityPrincipal("auditor-1", SecurityPrincipalType.System, partyReference: null);
    }
}
