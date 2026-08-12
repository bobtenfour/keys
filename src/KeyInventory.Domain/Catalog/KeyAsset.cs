namespace KeyInventory.Domain.Catalog;

/// <summary>
/// One physical key copy under a KEY #. Custody (Loan/Return) targets this aggregate.
/// Rooms and KeyType are derived from the parent KeyAccessPattern.
/// </summary>
public sealed class KeyAsset
{
    public KeyAsset(Guid keyAssetId, KeyAccessPattern accessPattern, string medecoKeyCode)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAssetId = keyAssetId;
        AccessPattern = accessPattern ?? throw new ArgumentNullException(nameof(accessPattern));
        if (!accessPattern.IsActive)
        {
            throw new InvalidOperationException("Physical key copy cannot reference an inactive KEY #.");
        }

        MedecoKeyCode = CatalogText.Require(medecoKeyCode, nameof(medecoKeyCode));
        IsActive = true;
    }

    public Guid KeyAssetId { get; }

    public KeyAccessPattern AccessPattern { get; }

    public string KeyNumber => AccessPattern.KeyNumber;

    public string MedecoKeyCode { get; }

    public KeyType KeyType => AccessPattern.KeyType;

    /// <summary>
    /// Rooms opened — derived solely from parent KEY #.
    /// </summary>
    public IReadOnlyCollection<string> OpenedRoomCodes => AccessPattern.OpenedRoomCodes;

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
