using KeyInventory.Application.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed record KeyAssetListItem(
    string CatalogKeyCode,
    string TypeCode,
    bool IsActive,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);
