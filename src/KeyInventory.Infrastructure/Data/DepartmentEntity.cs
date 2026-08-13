namespace KeyInventory.Infrastructure.Data;

public sealed class DepartmentEntity
{
    public Guid DepartmentId { get; set; }

    public string DepartmentCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
