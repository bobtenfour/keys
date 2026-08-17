namespace KeyInventory.Infrastructure.Data;

public sealed class RoomEntity
{
    public string RoomCode { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DepartmentEntity Department { get; set; } = null!;
}
