namespace KeyInventory.Domain.Workforce;

/// <summary>
/// Workforce Eligibility boundary — Room assignment for key-issue justification.
/// Links an Active WorkforceMember to an Active Room in the same Department.
/// Technical identity is WorkAssignmentId; no operator-facing assignment code.
/// </summary>
public sealed class WorkAssignment
{
    public WorkAssignment(
        Guid workAssignmentId,
        string workforceMemberCode,
        string roomCode)
    {
        if (workAssignmentId == Guid.Empty)
        {
            throw new ArgumentException("WorkAssignmentId is required.", nameof(workAssignmentId));
        }

        WorkAssignmentId = workAssignmentId;
        WorkforceMemberCode = WorkforceText.Require(workforceMemberCode, nameof(workforceMemberCode));
        RoomCode = WorkforceText.Require(roomCode, nameof(roomCode));
        IsActive = true;
    }

    public Guid WorkAssignmentId { get; }

    public string WorkforceMemberCode { get; }

    public string RoomCode { get; }

    public bool IsActive { get; private set; }

    public void End()
    {
        IsActive = false;
    }
}
