namespace KeyInventory.Domain.Workforce;

public sealed class Department
{
    public Department(string departmentCode)
    {
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
        IsActive = true;
    }

    public string DepartmentCode { get; }

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
