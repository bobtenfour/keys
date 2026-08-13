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

public interface IUpdateDepartmentCodeUseCase
{
    Task ExecuteAsync(Guid departmentId, string newDepartmentCode, CancellationToken cancellationToken);
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
        if (await _workforce.DepartmentExistsByCodeAsync(departmentCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A department with this code already exists.");
        }

        Department department = new(Guid.NewGuid(), departmentCode);
        _audit.Stage(
            OperatorAuditActions.DepartmentCreated,
            OperatorAuditSubjects.Department,
            department.DepartmentCode,
            $"DepartmentId={department.DepartmentId:D}");
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

public sealed class UpdateDepartmentCodeUseCase : IUpdateDepartmentCodeUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateDepartmentCodeUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        Guid departmentId,
        string newDepartmentCode,
        CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newDepartmentCode);
        string trimmedCode = newDepartmentCode.Trim();

        Department? department = await _workforce.FindDepartmentAsync(departmentId, cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        if (!string.Equals(department.DepartmentCode, trimmedCode, StringComparison.Ordinal)
            && await _workforce.DepartmentExistsByCodeAsync(trimmedCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A department with this code already exists.");
        }

        string oldCode = department.DepartmentCode;
        department.RenameCode(trimmedCode);
        _audit.Stage(
            OperatorAuditActions.DepartmentCodeChanged,
            OperatorAuditSubjects.Department,
            department.DepartmentCode,
            $"DepartmentId={department.DepartmentId:D}; OldCode={oldCode}; NewCode={department.DepartmentCode}");
        await _workforce.UpdateDepartmentAsync(department, cancellationToken).ConfigureAwait(false);
    }
}
