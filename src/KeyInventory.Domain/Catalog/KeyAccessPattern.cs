namespace KeyInventory.Domain.Catalog;

/// <summary>
/// KEY # / shared access-pattern aggregate.
/// Classification defines access: Regular opens exactly one Room; Master opens all Rooms.
/// Does not own Department; Departments of a KEY # are derived through opened Rooms.
/// </summary>
public sealed class KeyAccessPattern
{
    public KeyAccessPattern(string keyNumber, KeyAccessClassification classification, string? regularRoomCode)
    {
        KeyNumber = CatalogText.Require(keyNumber, nameof(keyNumber));
        Classification = RequireClassification(classification);
        RoomCode = RequireRoomCodeForClassification(classification, regularRoomCode);
        IsActive = true;
    }

    public string KeyNumber { get; }

    public KeyAccessClassification Classification { get; }

    /// <summary>
    /// Room opened by a Regular KEY #. Always null for Master.
    /// </summary>
    public string? RoomCode { get; }

    /// <summary>
    /// True when Classification is Master — access derives all current Rooms without storing them.
    /// </summary>
    public bool OpensAllRooms => Classification == KeyAccessClassification.Master;

    public bool IsActive { get; private set; }

    /// <summary>
    /// Stored Room codes on the pattern. Regular returns the single RoomCode;
    /// Master returns empty (Application expands to all Rooms when needed).
    /// </summary>
    public IReadOnlyCollection<string> OpenedRoomCodes =>
        RoomCode is null
            ? Array.Empty<string>()
            : new[] { RoomCode };

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire(bool hasActivePhysicalCopies)
    {
        if (hasActivePhysicalCopies)
        {
            throw new InvalidOperationException(
                "KEY # cannot be retired while active physical key copies reference it.");
        }

        IsActive = false;
    }

    private static KeyAccessClassification RequireClassification(KeyAccessClassification classification)
    {
        if (classification is not (KeyAccessClassification.Regular or KeyAccessClassification.Master))
        {
            throw new ArgumentOutOfRangeException(
                nameof(classification),
                "KEY # classification must be Regular or Master.");
        }

        return classification;
    }

    private static string? RequireRoomCodeForClassification(
        KeyAccessClassification classification,
        string? regularRoomCode)
    {
        if (classification == KeyAccessClassification.Master)
        {
            if (!string.IsNullOrWhiteSpace(regularRoomCode))
            {
                throw new InvalidOperationException(
                    "Master KEY # cannot have a Room. Access derives all Rooms from Classification.");
            }

            return null;
        }

        return CatalogText.Require(regularRoomCode, nameof(regularRoomCode));
    }
}
