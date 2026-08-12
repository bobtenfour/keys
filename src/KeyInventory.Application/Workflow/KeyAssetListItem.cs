using KeyInventory.Application.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed record KeyAssetListItem(
    Guid KeyAssetId,
    string KeyNumber,
    string MedecoKeyCode,
    string TypeCode,
    bool IsActive,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public sealed record KeyAccessPatternListItem(
    string KeyNumber,
    string TypeCode,
    bool IsActive,
    int PhysicalCopyCount,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);
