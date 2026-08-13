namespace KeyInventory.Domain.Workforce;

public sealed class Department
{
    public Department(Guid departmentId, string departmentCode)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        DepartmentId = departmentId;
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
        IsActive = true;
    }

    public Guid DepartmentId { get; }

    public string DepartmentCode { get; private set; }

    public bool IsActive { get; private set; }

    public void RenameCode(string departmentCode)
    {
        DepartmentCode = WorkforceText.Require(departmentCode, nameof(departmentCode));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
