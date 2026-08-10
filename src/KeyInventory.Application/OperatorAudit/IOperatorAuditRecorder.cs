using KeyInventory.Domain;

namespace KeyInventory.Application.OperatorAudit;

public interface IOperatorAuditRecorder
{
    void Stage(
        string actionType,
        string subjectType,
        string subjectReference,
        string? details = null);
}

public sealed class OperatorAuditRecorder : IOperatorAuditRecorder
{
    private readonly IOperatorAuditPersistencePort _persistence;
    private readonly IOperatorIdentityAccessor _operatorIdentity;

    public OperatorAuditRecorder(
        IOperatorAuditPersistencePort persistence,
        IOperatorIdentityAccessor operatorIdentity)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _operatorIdentity = operatorIdentity ?? throw new ArgumentNullException(nameof(operatorIdentity));
    }

    public void Stage(
        string actionType,
        string subjectType,
        string subjectReference,
        string? details = null)
    {
        string operatorReference = _operatorIdentity.GetRequiredOperatorReference();
        DateTimeOffset occurredAtUtc = UtcTimestamp.Require(DateTimeOffset.UtcNow, "occurredAtUtc");

        OperatorAuditRecord record = new(
            $"AUD-{Guid.NewGuid():D}",
            occurredAtUtc,
            operatorReference.Trim(),
            RequireText(actionType, nameof(actionType)),
            RequireText(subjectType, nameof(subjectType)),
            RequireText(subjectReference, nameof(subjectReference)),
            string.IsNullOrWhiteSpace(details) ? string.Empty : details.Trim());

        _persistence.Stage(record);
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value.Trim();
    }
}
