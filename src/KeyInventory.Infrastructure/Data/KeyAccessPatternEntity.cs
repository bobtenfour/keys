namespace KeyInventory.Infrastructure.Data;

public sealed class KeyAccessPatternEntity
{
    public string KeyNumber { get; set; } = string.Empty;

    public string KeyTypeCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public KeyTypeEntity KeyType { get; set; } = null!;
}
