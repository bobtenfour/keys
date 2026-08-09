namespace KeyInventory.Infrastructure.Data;

public sealed class KeyRoomAssignmentEntity
{
    public string CatalogKeyCode { get; set; } = string.Empty;

    public string RoomCode { get; set; } = string.Empty;

    public KeyAssetEntity KeyAsset { get; set; } = null!;

    public RoomEntity Room { get; set; } = null!;
}
