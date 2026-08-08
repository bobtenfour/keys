namespace KeyInventory.Infrastructure.Data;

public sealed class RoomEntity
{
    public string RoomCode { get; set; } = string.Empty;

    public string BuildingCode { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public BuildingEntity Building { get; set; } = null!;
}
