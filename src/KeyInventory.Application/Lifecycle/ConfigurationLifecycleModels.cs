using KeyInventory.Domain.Catalog;

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
    Guid DepartmentId,
    string DepartmentCode,
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
    Guid WorkAssignmentId,
    string WorkforceMemberCode,
    string RoomCode,
    bool IsActive,
    LifecycleCapabilities Capabilities);

public sealed record KeyAssetLifecycleItem(
    Guid KeyAssetId,
    string KeyNumber,
    string MedecoKeyCode,
    KeyAccessClassification Classification,
    KeyPhysicalCondition Condition,
    string AvailabilityStatus,
    bool CanMarkLost,
    bool CanDestroy,
    bool CanReplace,
    LifecycleCapabilities Capabilities);

public sealed record KeyAccessPatternLifecycleItem(
    string KeyNumber,
    KeyAccessClassification Classification,
    bool IsActive,
    int PhysicalCopyCount,
    LifecycleCapabilities Capabilities);
