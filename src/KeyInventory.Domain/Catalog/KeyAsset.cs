namespace KeyInventory.Domain.Catalog;

public sealed class KeyAsset
{
    private readonly HashSet<string> _openedRoomCodes = new(StringComparer.Ordinal);

    public KeyAsset(string catalogKeyCode, KeyType keyType, KeySeries? keySeries = null, Lock? intendedLock = null)
    {
        CatalogKeyCode = CatalogText.Require(catalogKeyCode, nameof(catalogKeyCode));
        KeyType = RequireActiveKeyType(keyType);
        KeySeries = RequireActiveKeySeries(keySeries);
        IntendedLock = RequireActiveLock(intendedLock);
        IsActive = true;
    }

    public string CatalogKeyCode { get; }

    public KeyType KeyType { get; private set; }

    public KeySeries? KeySeries { get; private set; }

    /// <summary>
    /// Optional catalog Lock identity only. Not room-opening authority.
    /// </summary>
    public Lock? IntendedLock { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Current Room codes this physical key opens. Building is derived through Room outside KeyAsset.
    /// </summary>
    public IReadOnlyCollection<string> OpenedRoomCodes => _openedRoomCodes;

    public void AssignKeyType(KeyType keyType)
    {
        KeyType = RequireActiveKeyType(keyType);
    }

    public void AssignKeySeries(KeySeries? keySeries)
    {
        KeySeries = RequireActiveKeySeries(keySeries);
    }

    public void AssignIntendedLock(Lock? intendedLock)
    {
        IntendedLock = RequireActiveLock(intendedLock);
    }

    public void AssignOpenedRoom(string roomCode)
    {
        string normalized = CatalogText.Require(roomCode, nameof(roomCode));
        if (!_openedRoomCodes.Add(normalized))
        {
            throw new InvalidOperationException("A current Key-to-Room assignment for this Key and Room already exists.");
        }
    }

    public void RemoveOpenedRoom(string roomCode)
    {
        string normalized = CatalogText.Require(roomCode, nameof(roomCode));
        if (!_openedRoomCodes.Remove(normalized))
        {
            throw new InvalidOperationException("The Key-to-Room assignment was not found.");
        }
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }

    private static KeyType RequireActiveKeyType(KeyType keyType)
    {
        ArgumentNullException.ThrowIfNull(keyType);

        if (!keyType.IsActive)
        {
            throw new InvalidOperationException("KeyAsset cannot reference an inactive KeyType for new catalog assignment.");
        }

        return keyType;
    }

    private static KeySeries? RequireActiveKeySeries(KeySeries? keySeries)
    {
        if (keySeries is not null && !keySeries.IsActive)
        {
            throw new InvalidOperationException("KeyAsset cannot reference an inactive KeySeries for new catalog assignment.");
        }

        return keySeries;
    }

    private static Lock? RequireActiveLock(Lock? intendedLock)
    {
        if (intendedLock is not null && !intendedLock.IsActive)
        {
            throw new InvalidOperationException("KeyAsset cannot reference an inactive Lock for new catalog assignment.");
        }

        if (intendedLock is not null && !intendedLock.Location.IsActive)
        {
            throw new InvalidOperationException("KeyAsset cannot reference an inactive Location for new catalog assignment.");
        }

        return intendedLock;
    }
}
