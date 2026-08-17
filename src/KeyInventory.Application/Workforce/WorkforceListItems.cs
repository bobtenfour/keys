namespace KeyInventory.Application.Workforce;

public sealed record DepartmentListItem(Guid DepartmentId, string DepartmentCode, bool IsActive);

public sealed record RoomListItem(
    string RoomCode,
    string RoomNumber,
    string Description,
    Guid DepartmentId,
    string DepartmentCode,
    bool IsActive);

public sealed record PartyListItem(
    string PartyCode,
    string FirstName,
    string LastName,
    string Uin);

public sealed record WorkforceMemberListItem(
    string WorkforceMemberCode,
    string PartyCode,
    string FirstName,
    string LastName,
    string Uin,
    string WorkforceType,
    string DepartmentCode,
    string Status);

public sealed record WorkAssignmentListItem(
    Guid WorkAssignmentId,
    string WorkforceMemberCode,
    string RoomCode,
    bool IsActive);

public sealed record OutstandingReturnObligationItem(
    string LoanCode,
    string KeyNumber,
    string MedecoKeyCode,
    string BorrowerPartyReference,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc);

public sealed record ActiveWorkAssignmentWithRoomDepartment(
    Guid WorkAssignmentId,
    string RoomCode,
    Guid RoomDepartmentId,
    string RoomDepartmentCode);
