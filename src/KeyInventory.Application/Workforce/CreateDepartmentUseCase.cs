using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateDepartmentUseCase
{
    Task ExecuteAsync(string organizationCode, string departmentCode, CancellationToken cancellationToken);
}

public interface IListDepartmentsUseCase
{
    Task<IReadOnlyList<DepartmentListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreateDepartmentUseCase : ICreateDepartmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CreateDepartmentUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string organizationCode, string departmentCode, CancellationToken cancellationToken)
    {
        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            throw new InvalidOperationException("The organization was not found.");
        }

        if (!organization.IsActive)
        {
            throw new InvalidOperationException("Department cannot reference an inactive Organization.");
        }

        if (await _workforce.DepartmentExistsAsync(organization.OrganizationCode, departmentCode, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("A department with this code already exists in the organization.");
        }

        Department department = new(departmentCode, organization);
        _audit.Stage(
            OperatorAuditActions.DepartmentCreated,
            OperatorAuditSubjects.Department,
            $"{organization.OrganizationCode}/{department.DepartmentCode}",
            $"Organization={organization.OrganizationCode}; Department={department.DepartmentCode}");
        await _workforce.AddDepartmentAsync(department, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class ListDepartmentsUseCase : IListDepartmentsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListDepartmentsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<DepartmentListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListDepartmentsAsync(cancellationToken);
    }
}
