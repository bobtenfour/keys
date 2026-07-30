using KeyInventory.Domain.Audit;

namespace KeyInventory.Application.Audit;

public interface IAuditEventLookupPort
{
    ValueTask<AuditEvent?> FindByAuditEventCodeAsync(
        string auditEventCode,
        CancellationToken cancellationToken);
}
