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

    public CreateDepartmentUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
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

        await _workforce.AddDepartmentAsync(new Department(departmentCode, organization), cancellationToken)
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
