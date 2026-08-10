namespace KeyInventory.Application.OperatorAudit;

public interface IOperatorAuditPersistencePort
{
    /// <summary>
    /// Stages an append-only audit row on the shared SQL Server unit of work.
    /// Must be flushed by the same SaveChanges that persists the business mutation.
    /// </summary>
    void Stage(OperatorAuditRecord record);

    Task<IReadOnlyList<OperatorAuditTrailItem>> QueryAsync(
        OperatorAuditTrailQuery query,
        CancellationToken cancellationToken);
}
