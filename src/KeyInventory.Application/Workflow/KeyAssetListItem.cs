using KeyInventory.Application.Catalog;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed record KeyAssetListItem(
    Guid KeyAssetId,
    string KeyNumber,
    string MedecoKeyCode,
    KeyAccessClassification Classification,
    KeyPhysicalCondition Condition,
    Guid? ReplacesKeyAssetId,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public sealed record KeyAccessPatternListItem(
    string KeyNumber,
    KeyAccessClassification Classification,
    bool IsActive,
    int PhysicalCopyCount,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);
