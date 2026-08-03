using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Identity;
using KeyInventory.Domain.Loans;

namespace KeyInventory.Domain.Audit;

/// <summary>
/// AuditEvent aggregate — authoritative immutable evidence of one business or security-relevant action.
/// Does not own authentication, authorization, policy, custody, lifecycle, catalog, Party, loan, or return authority.
/// </summary>
public sealed class AuditEvent
{
    public AuditEvent(
        string auditEventCode,
        string actionType,
        DateTimeOffset occurredAtUtc,
        SecurityPrincipal actingSecurityPrincipal,
        string? partyReference = null,
        KeyAsset? subjectKeyAsset = null,
        Loan? subjectLoan = null,
        Return? subjectReturn = null)
    {
        AuditEventCode = AuditText.Require(auditEventCode, nameof(auditEventCode));
        ActionType = AuditText.Require(actionType, nameof(actionType));
        ActingSecurityPrincipal = actingSecurityPrincipal
            ?? throw new ArgumentNullException(nameof(actingSecurityPrincipal));

        if (!string.IsNullOrWhiteSpace(partyReference))
        {
            PartyReference = partyReference.Trim();
        }

        OccurredAtUtc = UtcTimestamp.Require(occurredAtUtc, nameof(occurredAtUtc));
        SubjectKeyAsset = subjectKeyAsset;
        SubjectLoan = subjectLoan;
        SubjectReturn = subjectReturn;
    }

    public string AuditEventCode { get; }

    public string ActionType { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public SecurityPrincipal ActingSecurityPrincipal { get; }

    public string? PartyReference { get; }

    public KeyAsset? SubjectKeyAsset { get; }

    public Loan? SubjectLoan { get; }

    public Return? SubjectReturn { get; }
}
