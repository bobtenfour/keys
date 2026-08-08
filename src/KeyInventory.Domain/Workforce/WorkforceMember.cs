namespace KeyInventory.Domain.Workforce;

/// <summary>
/// Workforce Eligibility boundary — workforce relationship and eligibility authority.
/// References Party identity; does not own FirstName, LastName, or UIN.
/// </summary>
public sealed class WorkforceMember
{
    public WorkforceMember(
        string workforceMemberCode,
        string partyCode,
        WorkforceType workforceType,
        string organizationCode,
        string departmentCode,
        string responsibleManagerWorkforceMemberCode)
    {
        WorkforceMemberCode = WorkforceText.Require(workforceMemberCode, nameof(workforceMemberCode));
        PartyCode = WorkforceText.Require(partyCode, nameof(partyCode));
        WorkforceType = RequireWorkforceType(workforceType);
        OrganizationCode = WorkforceText.Require(organizationCode, nameof(organizationCode));
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
        ResponsibleManagerWorkforceMemberCode = WorkforceText.Require(
            responsibleManagerWorkforceMemberCode,
            nameof(responsibleManagerWorkforceMemberCode));

        if (string.Equals(WorkforceMemberCode, ResponsibleManagerWorkforceMemberCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ResponsibleManager must reference a different WorkforceMember.");
        }

        Status = WorkforceMemberStatus.Active;
    }

    public string WorkforceMemberCode { get; }

    public string PartyCode { get; }

    public WorkforceType WorkforceType { get; private set; }

    public string OrganizationCode { get; private set; }

    public string DepartmentCode { get; private set; }

    public string ResponsibleManagerWorkforceMemberCode { get; private set; }

    public WorkforceMemberStatus Status { get; private set; }

    public void AssignOrganizationAndDepartment(string organizationCode, string departmentCode)
    {
        if (Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change Organization or Department.");
        }

        OrganizationCode = WorkforceText.Require(organizationCode, nameof(organizationCode));
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
    }

    public void AssignResponsibleManager(string responsibleManagerWorkforceMemberCode)
    {
        if (Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change ResponsibleManager.");
        }

        string managerCode = WorkforceText.Require(
            responsibleManagerWorkforceMemberCode,
            nameof(responsibleManagerWorkforceMemberCode));

        if (string.Equals(WorkforceMemberCode, managerCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ResponsibleManager must reference a different WorkforceMember.");
        }

        ResponsibleManagerWorkforceMemberCode = managerCode;
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
