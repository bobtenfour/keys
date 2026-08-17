namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAssetEntity
{
    public Guid KeyAssetId { get; set; }

    public string KeyNumber { get; set; } = string.Empty;

    public string MedecoKeyCode { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;

    public Guid? ReplacesKeyAssetId { get; set; }

    public KeyAccessPatternEntity AccessPattern { get; set; } = null!;

    public KeyAssetEntity? ReplacesKeyAsset { get; set; }
}
