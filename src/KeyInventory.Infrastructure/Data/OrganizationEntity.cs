namespace KeyInventory.Infrastructure.Data;

public sealed class OrganizationEntity
{
    public string OrganizationCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
