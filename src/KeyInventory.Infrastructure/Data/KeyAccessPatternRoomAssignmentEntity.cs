namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAccessPatternRoomAssignmentEntity
{
    public string KeyNumber { get; set; } = string.Empty;

    public string RoomCode { get; set; } = string.Empty;

    public KeyAccessPatternEntity KeyAccessPattern { get; set; } = null!;

    public RoomEntity Room { get; set; } = null!;
}
