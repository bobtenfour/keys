namespace KeyInventory.Infrastructure.Data;

public sealed class BuildingEntity
{
    public string BuildingCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
