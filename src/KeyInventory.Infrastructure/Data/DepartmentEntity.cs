namespace KeyInventory.Infrastructure.Data;

public sealed class DepartmentEntity
{
    public string OrganizationCode { get; set; } = string.Empty;

    public string DepartmentCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public OrganizationEntity Organization { get; set; } = null!;
}
