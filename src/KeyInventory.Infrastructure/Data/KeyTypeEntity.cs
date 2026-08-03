namespace KeyInventory.Infrastructure.Data;

public sealed class KeyTypeEntity
{
    public string TypeCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
