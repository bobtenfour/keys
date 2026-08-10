namespace KeyInventory.Application.OperatorAudit;

public static class OperatorAuditActions
{
    public const string KeyRegistered = "Key registered";
    public const string KeyRoomAssignmentAdded = "Key↔Room assignment added";
    public const string KeyRoomAssignmentRemoved = "Key↔Room assignment removed";
    public const string KeyIssued = "Key issued";
    public const string KeyReturned = "Key returned";
    public const string WorkforceMemberCreated = "Workforce Member created";
    public const string WorkforceMemberMaintained = "Workforce Member maintained";
    public const string WorkforceMemberTerminated = "Workforce Member terminated";
    public const string WorkAssignmentCreated = "WorkAssignment created";
    public const string WorkAssignmentEnded = "WorkAssignment ended";
    public const string WorkAssignmentPrimaryChanged = "WorkAssignment primary changed";
    public const string OrganizationCreated = "Organization created";
    public const string OrganizationActivated = "Organization activated";
    public const string OrganizationRetired = "Organization retired";
    public const string DepartmentCreated = "Department created";
    public const string DepartmentActivated = "Department activated";
    public const string DepartmentRetired = "Department retired";
    public const string BuildingCreated = "Building created";
    public const string BuildingActivated = "Building activated";
    public const string BuildingRetired = "Building retired";
    public const string RoomCreated = "Room created";
    public const string RoomActivated = "Room activated";
    public const string RoomRetired = "Room retired";
    public const string KeyTypeCreated = "KeyType created";
    public const string KeyTypeActivated = "KeyType activated";
    public const string KeyTypeRetired = "KeyType retired";
}

public static class OperatorAuditSubjects
{
    public const string Key = "Key";
    public const string KeyRoomAssignment = "Key↔Room";
    public const string Loan = "Loan";
    public const string Return = "Return";
    public const string WorkforceMember = "Workforce Member";
    public const string WorkAssignment = "Work Assignment";
    public const string Organization = "Organization";
    public const string Department = "Department";
    public const string Building = "Building";
    public const string Room = "Room";
    public const string KeyType = "Key Type";
}

public sealed record OperatorAuditRecord(
    string AuditRecordId,
    DateTimeOffset OccurredAtUtc,
    string OperatorReference,
    string ActionType,
    string SubjectType,
    string SubjectReference,
    string Details);

public sealed record OperatorAuditTrailItem(
    string AuditRecordId,
    DateTimeOffset OccurredAtUtc,
    string OperatorReference,
    string ActionType,
    string SubjectType,
    string SubjectReference,
    string Details);

public sealed record OperatorAuditTrailQuery(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? OperatorReference,
    string? ActionType,
    string? SubjectReference);
