namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAccessPatternEntity
{
    public string KeyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Persisted as a case-sensitive string ("Regular" | "Master") for schema clarity.
    /// The KeyAccessClassification enum is the sole classification authority.
    /// </summary>
    public string Classification { get; set; } = string.Empty;

    /// <summary>
    /// Room opened by a Regular KEY #. Null for Master (access derives all Rooms).
    /// </summary>
    public string? RoomCode { get; set; }

    public bool IsActive { get; set; }

    public RoomEntity? Room { get; set; }
}
