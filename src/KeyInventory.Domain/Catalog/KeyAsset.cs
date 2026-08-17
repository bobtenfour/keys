namespace KeyInventory.Domain.Catalog;

/// <summary>
/// One physical key under a KEY #. Custody (Loan) targets this aggregate.
/// Rooms and Regular/Master classification are derived from the parent KeyAccessPattern.
/// Physical condition is Active | Lost | Destroyed. Available/Issued are derived, not stored.
/// </summary>
public sealed class KeyAsset
{
    public KeyAsset(
        Guid keyAssetId,
        KeyAccessPattern accessPattern,
        string medecoKeyCode,
        Guid? replacesKeyAssetId = null)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAssetId = keyAssetId;
        AccessPattern = accessPattern ?? throw new ArgumentNullException(nameof(accessPattern));
        if (!accessPattern.IsActive)
        {
            throw new InvalidOperationException("A key cannot reference an inactive KEY #.");
        }

        MedecoKeyCode = CatalogText.Require(medecoKeyCode, nameof(medecoKeyCode));
        Condition = KeyPhysicalCondition.Active;
        ReplacesKeyAssetId = replacesKeyAssetId;
        if (replacesKeyAssetId is Guid sourceId && sourceId == Guid.Empty)
        {
            throw new ArgumentException("ReplacesKeyAssetId must be a real KeyAsset identity when set.", nameof(replacesKeyAssetId));
        }

        if (replacesKeyAssetId == keyAssetId)
        {
            throw new ArgumentException("A key cannot replace itself.", nameof(replacesKeyAssetId));
        }
    }

    public Guid KeyAssetId { get; }

    public KeyAccessPattern AccessPattern { get; }

    public string KeyNumber => AccessPattern.KeyNumber;

    public string MedecoKeyCode { get; }

    public KeyAccessClassification Classification => AccessPattern.Classification;

    /// <summary>
    /// Stored Room codes on the parent KEY # (Regular: one Room; Master: empty — expands in Application).
    /// KeyAsset never stores Room authority.
    /// </summary>
    public IReadOnlyCollection<string> OpenedRoomCodes => AccessPattern.OpenedRoomCodes;

    public KeyPhysicalCondition Condition { get; private set; }

    /// <summary>
    /// When set, this key was created as Replacement of a Lost KeyAsset (same KEY #).
    /// </summary>
    public Guid? ReplacesKeyAssetId { get; }

    public bool IsIssuableCondition => Condition == KeyPhysicalCondition.Active;

    public void MarkLost()
    {
        if (Condition != KeyPhysicalCondition.Active)
        {
            throw new InvalidOperationException("Only an Active key may be marked Lost.");
        }

        Condition = KeyPhysicalCondition.Lost;
    }

    public void Destroy()
    {
        if (Condition is not (KeyPhysicalCondition.Active or KeyPhysicalCondition.Lost))
        {
            throw new InvalidOperationException("Only an Active or Lost key may be Destroyed.");
        }

        Condition = KeyPhysicalCondition.Destroyed;
    }

    /// <summary>
    /// Reconstitutes Condition from persistence without replaying transitions.
    /// </summary>
    public static KeyAsset Rehydrate(
        Guid keyAssetId,
        KeyAccessPattern accessPattern,
        string medecoKeyCode,
        KeyPhysicalCondition condition,
        Guid? replacesKeyAssetId)
    {
        KeyAsset asset = new(keyAssetId, accessPattern, medecoKeyCode, replacesKeyAssetId);
        asset.Condition = condition switch
        {
            KeyPhysicalCondition.Active => KeyPhysicalCondition.Active,
            KeyPhysicalCondition.Lost => KeyPhysicalCondition.Lost,
            KeyPhysicalCondition.Destroyed => KeyPhysicalCondition.Destroyed,
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };
        return asset;
    }
}
