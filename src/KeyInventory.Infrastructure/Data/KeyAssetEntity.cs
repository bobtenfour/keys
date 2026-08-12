namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAssetEntity
{
    public Guid KeyAssetId { get; set; }

    public string KeyNumber { get; set; } = string.Empty;

    public string MedecoKeyCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public KeyAccessPatternEntity AccessPattern { get; set; } = null!;
}
