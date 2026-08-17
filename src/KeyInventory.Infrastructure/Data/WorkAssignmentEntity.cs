namespace KeyInventory.Infrastructure.Data;

public sealed class WorkAssignmentEntity
{
    public Guid WorkAssignmentId { get; set; }

    public string WorkforceMemberCode { get; set; } = string.Empty;

    public string RoomCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public WorkforceMemberEntity WorkforceMember { get; set; } = null!;

    public RoomEntity Room { get; set; } = null!;
}
