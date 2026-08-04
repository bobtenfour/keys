namespace KeyInventory.Application.Workflow;

public sealed record KeyAssetListItem(
    string CatalogKeyCode,
    string TypeCode,
    bool IsActive);
