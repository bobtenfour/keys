namespace KeyInventory.Domain.Workforce;

/// <summary>
/// Workforce Eligibility boundary — workforce relationship and eligibility authority.
/// References Party identity; does not own FirstName, LastName, or UIN.
/// Organization and ResponsibleManager are not active authorities.
/// </summary>
public sealed class WorkforceMember
{
    public WorkforceMember(
        string workforceMemberCode,
        string partyCode,
        WorkforceType workforceType,
        Guid departmentId)
    {
        WorkforceMemberCode = WorkforceText.Require(workforceMemberCode, nameof(workforceMemberCode));
        PartyCode = WorkforceText.Require(partyCode, nameof(partyCode));
        WorkforceType = RequireWorkforceType(workforceType);
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        DepartmentId = departmentId;
        Status = WorkforceMemberStatus.Active;
    }

    public string WorkforceMemberCode { get; }

    public string PartyCode { get; }

    public WorkforceType WorkforceType { get; private set; }

    public Guid DepartmentId { get; private set; }

    public WorkforceMemberStatus Status { get; private set; }

    public void AssignDepartment(Guid departmentId)
    {
        if (Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change Department.");
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        DepartmentId = departmentId;
    }

    public void ChangeWorkforceType(WorkforceType workforceType)
    {
        if (Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change WorkforceType.");
        }

        WorkforceType = RequireWorkforceType(workforceType);
    }

    /// <summary>
    /// Terminates the workforce relationship. Blocks future key issues.
    /// Does not mutate Loan, Return, Audit, Custody, or Lifecycle.
    /// </summary>
    public void Terminate()
    {
        if (Status == WorkforceMemberStatus.Terminated)
        {
            throw new InvalidOperationException("WorkforceMember is already Terminated.");
        }

        Status = WorkforceMemberStatus.Terminated;
    }

    private static WorkforceType RequireWorkforceType(WorkforceType workforceType)
    {
        if (workforceType is not (WorkforceType.Employee or WorkforceType.Contractor))
        {
            throw new ArgumentOutOfRangeException(nameof(workforceType), "WorkforceType must be Employee or Contractor.");
        }

        return workforceType;
    }
}
