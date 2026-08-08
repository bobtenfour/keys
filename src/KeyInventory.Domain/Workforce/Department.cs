namespace KeyInventory.Domain.Workforce;

public sealed class Department
{
    public Department(string departmentCode, Organization organization)
    {
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationCode = organization.OrganizationCode;
        IsActive = true;
    }

    public string DepartmentCode { get; }

    public Organization Organization { get; private set; }

    public string OrganizationCode { get; }

    public bool IsActive { get; private set; }

    public void Activate(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        if (!string.Equals(organization.OrganizationCode, OrganizationCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Department activation must use the owning Organization.");
        }

        if (!organization.IsActive)
        {
            throw new InvalidOperationException("Department cannot be active in an inactive Organization.");
        }

        Organization = organization;
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
