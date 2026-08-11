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
        string departmentCode)
    {
        WorkforceMemberCode = WorkforceText.Require(workforceMemberCode, nameof(workforceMemberCode));
        PartyCode = WorkforceText.Require(partyCode, nameof(partyCode));
        WorkforceType = RequireWorkforceType(workforceType);
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
        Status = WorkforceMemberStatus.Active;
    }

    public string WorkforceMemberCode { get; }

    public string PartyCode { get; }

    public WorkforceType WorkforceType { get; private set; }

    public string DepartmentCode { get; private set; }

    public WorkforceMemberStatus Status { get; private set; }

    public void AssignDepartment(string departmentCode)
    {
        if (Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change Department.");
        }

        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
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
