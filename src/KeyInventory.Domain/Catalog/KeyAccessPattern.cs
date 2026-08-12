namespace KeyInventory.Domain.Catalog;

/// <summary>
/// KEY # / shared access-pattern aggregate. Owns KeyType and Room openings for all physical copies.
/// </summary>
public sealed class KeyAccessPattern
{
    private readonly HashSet<string> _openedRoomCodes = new(StringComparer.Ordinal);

    public KeyAccessPattern(string keyNumber, KeyType keyType)
    {
        KeyNumber = CatalogText.Require(keyNumber, nameof(keyNumber));
        KeyType = RequireActiveKeyType(keyType);
        IsActive = true;
    }

    public string KeyNumber { get; }

    public KeyType KeyType { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Current Room codes opened by every physical copy under this KEY #.
    /// </summary>
    public IReadOnlyCollection<string> OpenedRoomCodes => _openedRoomCodes;

    public void AssignKeyType(KeyType keyType)
    {
        KeyType = RequireActiveKeyType(keyType);
    }

    public void AssignOpenedRoom(string roomCode)
    {
        string normalized = CatalogText.Require(roomCode, nameof(roomCode));
        if (!_openedRoomCodes.Add(normalized))
        {
            throw new InvalidOperationException("A current KEY # to Room assignment for this KEY # and Room already exists.");
        }
    }

    public void RemoveOpenedRoom(string roomCode)
    {
        string normalized = CatalogText.Require(roomCode, nameof(roomCode));
        if (!_openedRoomCodes.Remove(normalized))
        {
            throw new InvalidOperationException("The KEY # to Room assignment was not found.");
        }
    }

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

    private static KeyType RequireActiveKeyType(KeyType keyType)
    {
        ArgumentNullException.ThrowIfNull(keyType);

        if (!keyType.IsActive)
        {
            throw new InvalidOperationException("KEY # cannot reference an inactive KeyType for new catalog assignment.");
        }

        return keyType;
    }
}
