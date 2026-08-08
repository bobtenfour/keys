namespace KeyInventory.Domain.Workforce;

public sealed class Organization
{
    public Organization(string organizationCode)
    {
        OrganizationCode = WorkforceText.Require(organizationCode, nameof(organizationCode));
        IsActive = true;
    }

    public string OrganizationCode { get; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
