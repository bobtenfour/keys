namespace KeyInventory.Domain.Workforce;

/// <summary>
/// Workforce Eligibility boundary — Room assignment for key-issue justification.
/// </summary>
public sealed class WorkAssignment
{
    public WorkAssignment(
        string workAssignmentCode,
        string workforceMemberCode,
        string roomCode,
        bool isPrimary)
    {
        WorkAssignmentCode = WorkforceText.Require(workAssignmentCode, nameof(workAssignmentCode));
        WorkforceMemberCode = WorkforceText.Require(workforceMemberCode, nameof(workforceMemberCode));
        RoomCode = WorkforceText.Require(roomCode, nameof(roomCode));
        IsPrimary = isPrimary;
        IsActive = true;
    }

    public string WorkAssignmentCode { get; }

    public string WorkforceMemberCode { get; }

    public string RoomCode { get; }

    public bool IsPrimary { get; private set; }

    public bool IsActive { get; private set; }

    public void MarkPrimary()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Only an active WorkAssignment may be primary.");
        }

        IsPrimary = true;
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
    }

    public void End()
    {
        IsActive = false;
        IsPrimary = false;
    }
}
