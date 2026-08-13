namespace KeyInventory.Application.Lifecycle;

public sealed record DepartmentLifecycleItem(
    Guid DepartmentId,
    string DepartmentCode,
    bool IsActive,
    LifecycleCapabilities Capabilities);

public sealed record RoomLifecycleItem(
    string RoomCode,
    string RoomNumber,
    string Description,
    bool IsActive,
    LifecycleCapabilities Capabilities);

public sealed record WorkforceMemberLifecycleItem(
    string WorkforceMemberCode,
    string PartyCode,
    string FirstName,
    string LastName,
    string Uin,
    string WorkforceType,
    string DepartmentCode,
    string Status,
    LifecycleCapabilities Capabilities);

public sealed record WorkAssignmentLifecycleItem(
    string WorkAssignmentCode,
    string WorkforceMemberCode,
    string RoomCode,
    bool IsPrimary,
    bool IsActive,
    LifecycleCapabilities Capabilities);

public sealed record KeyTypeLifecycleItem(
    string TypeCode,
    bool IsActive,
    int ActiveKeyAccessPatternCount,
    LifecycleCapabilities Capabilities);

public sealed record KeyAssetLifecycleItem(
    Guid KeyAssetId,
    string KeyNumber,
    string MedecoKeyCode,
    string TypeCode,
    bool IsActive,
    string AvailabilityStatus,
    LifecycleCapabilities Capabilities);

public sealed record KeyAccessPatternLifecycleItem(
    string KeyNumber,
    string TypeCode,
    bool IsActive,
    int PhysicalCopyCount,
    LifecycleCapabilities Capabilities);
