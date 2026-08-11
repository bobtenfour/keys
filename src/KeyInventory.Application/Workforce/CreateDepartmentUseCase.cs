using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateDepartmentUseCase
{
    Task ExecuteAsync(string departmentCode, CancellationToken cancellationToken);
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

    public async Task ExecuteAsync(string departmentCode, CancellationToken cancellationToken)
    {
        if (await _workforce.DepartmentExistsAsync(departmentCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A department with this code already exists.");
        }

        Department department = new(departmentCode);
        _audit.Stage(
            OperatorAuditActions.DepartmentCreated,
            OperatorAuditSubjects.Department,
            department.DepartmentCode);
        await _workforce.AddDepartmentAsync(department, cancellationToken).ConfigureAwait(false);
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
