using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface IActivateOrganizationUseCase
{
    Task ExecuteAsync(string organizationCode, CancellationToken cancellationToken);
}

public interface IRetireOrganizationUseCase
{
    Task ExecuteAsync(string organizationCode, CancellationToken cancellationToken);
}

public interface IActivateDepartmentUseCase
{
    Task ExecuteAsync(string organizationCode, string departmentCode, CancellationToken cancellationToken);
}

public interface IRetireDepartmentUseCase
{
    Task ExecuteAsync(string organizationCode, string departmentCode, CancellationToken cancellationToken);
}

public interface IActivateBuildingUseCase
{
    Task ExecuteAsync(string buildingCode, CancellationToken cancellationToken);
}

public interface IRetireBuildingUseCase
{
    Task ExecuteAsync(string buildingCode, CancellationToken cancellationToken);
}

public interface IActivateRoomUseCase
{
    Task ExecuteAsync(string roomCode, CancellationToken cancellationToken);
}

public interface IRetireRoomUseCase
{
    Task ExecuteAsync(string roomCode, CancellationToken cancellationToken);
}

public interface IUpdateWorkforceMemberOrganizationDepartmentUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken);
}

public interface IUpdateWorkforceMemberResponsibleManagerUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string responsibleManagerWorkforceMemberCode,
        CancellationToken cancellationToken);
}

public interface IUpdateWorkforceMemberWorkforceTypeUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string workforceType,
        CancellationToken cancellationToken);
}

public interface IEndWorkAssignmentUseCase
{
    Task ExecuteAsync(string workAssignmentCode, CancellationToken cancellationToken);
}

public interface IMarkWorkAssignmentPrimaryUseCase
{
    Task ExecuteAsync(string workAssignmentCode, CancellationToken cancellationToken);
}

public interface IClearWorkAssignmentPrimaryUseCase
{
    Task ExecuteAsync(string workAssignmentCode, CancellationToken cancellationToken);
}

public sealed class ActivateOrganizationUseCase : IActivateOrganizationUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public ActivateOrganizationUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string organizationCode, CancellationToken cancellationToken)
    {
        Organization organization = await RequireOrganizationAsync(organizationCode, cancellationToken)
            .ConfigureAwait(false);
        organization.Activate();
        _audit.Stage(
            OperatorAuditActions.OrganizationActivated,
            OperatorAuditSubjects.Organization,
            organization.OrganizationCode);
        await _workforce.UpdateOrganizationAsync(organization, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Organization> RequireOrganizationAsync(string organizationCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationCode);
        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        return organization ?? throw new InvalidOperationException("The organization was not found.");
    }
}

public sealed class RetireOrganizationUseCase : IRetireOrganizationUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public RetireOrganizationUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string organizationCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationCode);
        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            throw new InvalidOperationException("The organization was not found.");
        }

        organization.Retire();
        _audit.Stage(
            OperatorAuditActions.OrganizationRetired,
            OperatorAuditSubjects.Organization,
            organization.OrganizationCode);
        await _workforce.UpdateOrganizationAsync(organization, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ActivateDepartmentUseCase : IActivateDepartmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public ActivateDepartmentUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            throw new InvalidOperationException("The organization was not found.");
        }

        Department? department = await _workforce
            .FindDepartmentAsync(organizationCode.Trim(), departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        department.Activate(organization);
        _audit.Stage(
            OperatorAuditActions.DepartmentActivated,
            OperatorAuditSubjects.Department,
            $"{department.OrganizationCode}/{department.DepartmentCode}",
            $"Organization={department.OrganizationCode}; Department={department.DepartmentCode}");
        await _workforce.UpdateDepartmentAsync(department, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class RetireDepartmentUseCase : IRetireDepartmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public RetireDepartmentUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        Department? department = await _workforce
            .FindDepartmentAsync(organizationCode.Trim(), departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        department.Retire();
        _audit.Stage(
            OperatorAuditActions.DepartmentRetired,
            OperatorAuditSubjects.Department,
            $"{department.OrganizationCode}/{department.DepartmentCode}",
            $"Organization={department.OrganizationCode}; Department={department.DepartmentCode}");
        await _workforce.UpdateDepartmentAsync(department, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ActivateBuildingUseCase : IActivateBuildingUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public ActivateBuildingUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string buildingCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buildingCode);
        Building? building = await _workforce.FindBuildingAsync(buildingCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (building is null)
        {
            throw new InvalidOperationException("The building was not found.");
        }

        building.Activate();
        _audit.Stage(
            OperatorAuditActions.BuildingActivated,
            OperatorAuditSubjects.Building,
            building.BuildingCode);
        await _workforce.UpdateBuildingAsync(building, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class RetireBuildingUseCase : IRetireBuildingUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public RetireBuildingUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string buildingCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buildingCode);
        Building? building = await _workforce.FindBuildingAsync(buildingCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (building is null)
        {
            throw new InvalidOperationException("The building was not found.");
        }

        building.Retire();
        _audit.Stage(
            OperatorAuditActions.BuildingRetired,
            OperatorAuditSubjects.Building,
            building.BuildingCode);
        await _workforce.UpdateBuildingAsync(building, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ActivateRoomUseCase : IActivateRoomUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public ActivateRoomUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        Room? room = await _workforce.FindRoomAsync(roomCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        Building? building = await _workforce.FindBuildingAsync(room.BuildingCode, cancellationToken)
            .ConfigureAwait(false);
        if (building is null)
        {
            throw new InvalidOperationException("The building for the room was not found.");
        }

        room.Activate(building);
        _audit.Stage(
            OperatorAuditActions.RoomActivated,
            OperatorAuditSubjects.Room,
            room.RoomCode);
        await _workforce.UpdateRoomAsync(room, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class RetireRoomUseCase : IRetireRoomUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public RetireRoomUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        Room? room = await _workforce.FindRoomAsync(roomCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        room.Retire();
        _audit.Stage(
            OperatorAuditActions.RoomRetired,
            OperatorAuditSubjects.Room,
            room.RoomCode);
        await _workforce.UpdateRoomAsync(room, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpdateWorkforceMemberOrganizationDepartmentUseCase
    : IUpdateWorkforceMemberOrganizationDepartmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateWorkforceMemberOrganizationDepartmentUseCase(
        IWorkforcePersistencePort workforce,
        IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        WorkforceMember member = await RequireActiveMemberAsync(workforceMemberCode.Trim(), cancellationToken)
            .ConfigureAwait(false);

        Organization? organization = await _workforce.FindOrganizationAsync(organizationCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || !organization.IsActive)
        {
            throw new InvalidOperationException("Organization must exist and be active.");
        }

        Department? department = await _workforce
            .FindDepartmentAsync(organizationCode.Trim(), departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException("Department must exist and be active in the organization.");
        }

        member.AssignOrganizationAndDepartment(organization.OrganizationCode, department.DepartmentCode);
        _audit.Stage(
            OperatorAuditActions.WorkforceMemberMaintained,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode,
            $"Organization={organization.OrganizationCode}; Department={department.DepartmentCode}");
        await _workforce.UpdateWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }

    private async Task<WorkforceMember> RequireActiveMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        if (member.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change Organization or Department.");
        }

        return member;
    }
}

public sealed class UpdateWorkforceMemberResponsibleManagerUseCase
    : IUpdateWorkforceMemberResponsibleManagerUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateWorkforceMemberResponsibleManagerUseCase(
        IWorkforcePersistencePort workforce,
        IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string responsibleManagerWorkforceMemberCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(responsibleManagerWorkforceMemberCode);

        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        if (member.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change ResponsibleManager.");
        }

        WorkforceMember? manager = await _workforce
            .FindWorkforceMemberAsync(responsibleManagerWorkforceMemberCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (manager is null || manager.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("ResponsibleManager must be an active WorkforceMember.");
        }

        member.AssignResponsibleManager(manager.WorkforceMemberCode);
        _audit.Stage(
            OperatorAuditActions.WorkforceMemberMaintained,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode,
            $"ResponsibleManager={manager.WorkforceMemberCode}");
        await _workforce.UpdateWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpdateWorkforceMemberWorkforceTypeUseCase : IUpdateWorkforceMemberWorkforceTypeUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateWorkforceMemberWorkforceTypeUseCase(
        IWorkforcePersistencePort workforce,
        IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string workforceType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceType);

        if (!Enum.TryParse(workforceType.Trim(), ignoreCase: true, out WorkforceType parsed)
            || parsed is not (WorkforceType.Employee or WorkforceType.Contractor))
        {
            throw new ArgumentException("WorkforceType must be Employee or Contractor.", nameof(workforceType));
        }

        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        if (member.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("Only an Active WorkforceMember may change WorkforceType.");
        }

        member.ChangeWorkforceType(parsed);
        _audit.Stage(
            OperatorAuditActions.WorkforceMemberMaintained,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode,
            $"WorkforceType={parsed}");
        await _workforce.UpdateWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EndWorkAssignmentUseCase : IEndWorkAssignmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public EndWorkAssignmentUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workAssignmentCode);
        WorkAssignment? assignment = await _workforce
            .FindWorkAssignmentAsync(workAssignmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            throw new InvalidOperationException("The work assignment was not found.");
        }

        assignment.End();
        _audit.Stage(
            OperatorAuditActions.WorkAssignmentEnded,
            OperatorAuditSubjects.WorkAssignment,
            assignment.WorkAssignmentCode);
        await _workforce.UpdateWorkAssignmentAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class MarkWorkAssignmentPrimaryUseCase : IMarkWorkAssignmentPrimaryUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public MarkWorkAssignmentPrimaryUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workAssignmentCode);
        WorkAssignment? assignment = await _workforce
            .FindWorkAssignmentAsync(workAssignmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            throw new InvalidOperationException("The work assignment was not found.");
        }

        if (!assignment.IsActive)
        {
            throw new InvalidOperationException("Only an active WorkAssignment may be primary.");
        }

        await _workforce.ClearPrimaryAssignmentsAsync(assignment.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        assignment.MarkPrimary();
        _audit.Stage(
            OperatorAuditActions.WorkAssignmentPrimaryChanged,
            OperatorAuditSubjects.WorkAssignment,
            assignment.WorkAssignmentCode,
            "Primary=true");
        await _workforce.UpdateWorkAssignmentAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ClearWorkAssignmentPrimaryUseCase : IClearWorkAssignmentPrimaryUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public ClearWorkAssignmentPrimaryUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workAssignmentCode);
        WorkAssignment? assignment = await _workforce
            .FindWorkAssignmentAsync(workAssignmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            throw new InvalidOperationException("The work assignment was not found.");
        }

        assignment.ClearPrimary();
        _audit.Stage(
            OperatorAuditActions.WorkAssignmentPrimaryChanged,
            OperatorAuditSubjects.WorkAssignment,
            assignment.WorkAssignmentCode,
            "Primary=false");
        await _workforce.UpdateWorkAssignmentAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}
