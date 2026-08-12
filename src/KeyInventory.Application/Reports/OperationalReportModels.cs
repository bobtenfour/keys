using KeyInventory.Application.Catalog;

namespace KeyInventory.Application.Reports;

public sealed record CurrentKeyHolderReportRow(
    string KeyNumber,
    string MedecoKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    string? WorkforceMemberCode,
    string? DepartmentCode,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    string Status);

public sealed record ActiveLoanReportRow(
    string KeyNumber,
    string MedecoKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    string? WorkforceMemberCode,
    string? DepartmentCode,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    string Status);

public sealed record OverdueKeyReportRow(
    string KeyNumber,
    string MedecoKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    string? WorkforceMemberCode,
    string? DepartmentCode,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    int DaysOverdue,
    string Status);

public sealed record MemberIssuedKeyReportRow(
    string KeyNumber,
    string MedecoKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    string Status);

public sealed record MemberReturnedKeyReportRow(
    string KeyNumber,
    string MedecoKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ReturnedAtUtc,
    string Status);

public sealed record KeysByWorkforceMemberReport(
    string WorkforceMemberCode,
    IReadOnlyList<MemberIssuedKeyReportRow> IssuedKeys,
    IReadOnlyList<MemberReturnedKeyReportRow> ReturnedKeys);

public sealed record KeyHistoryReportRow(
    string LoanCode,
    string KeyNumber,
    string MedecoKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    string Status);

public sealed record OutstandingWorkforceKeyReportRow(
    string WorkforceMemberCode,
    string WorkforceMemberStatus,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    string DepartmentCode,
    string KeyNumber,
    string MedecoKeyCode,
    string LoanCode,
    DateTimeOffset DueAtUtc);

public sealed record KeyCatalogReportRow(
    string KeyNumber,
    string MedecoKeyCode,
    string TypeCode,
    bool IsActive,
    string AvailabilityStatus,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public sealed record WorkforceMemberReportOption(
    string WorkforceMemberCode,
    string FirstName,
    string LastName,
    string Uin,
    string Status);
