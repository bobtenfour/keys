namespace KeyInventory.Infrastructure.Data;

public sealed class WorkAssignmentEntity
{
    public string WorkAssignmentCode { get; set; } = string.Empty;

    public string WorkforceMemberCode { get; set; } = string.Empty;

    public string RoomCode { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }
}
