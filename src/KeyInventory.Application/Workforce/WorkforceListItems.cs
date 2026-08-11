namespace KeyInventory.Application.Workforce;

public sealed record DepartmentListItem(string DepartmentCode, bool IsActive);

public sealed record RoomListItem(
    string RoomCode,
    string RoomNumber,
    string Description,
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
    string WorkAssignmentCode,
    string WorkforceMemberCode,
    string RoomCode,
    bool IsPrimary,
    bool IsActive);

public sealed record OutstandingReturnObligationItem(
    string LoanCode,
    string CatalogKeyCode,
    string BorrowerPartyReference,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc);
