namespace KeyInventory.Infrastructure.Data;

public sealed class OperatorAuditRecordEntity
{
    public string AuditRecordId { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string OperatorReference { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string SubjectType { get; set; } = string.Empty;

    public string SubjectReference { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}
