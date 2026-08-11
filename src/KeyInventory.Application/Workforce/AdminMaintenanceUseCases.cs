using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface IActivateDepartmentUseCase
{
    Task ExecuteAsync(string departmentCode, CancellationToken cancellationToken);
}

public interface IRetireDepartmentUseCase
{
    Task ExecuteAsync(string departmentCode, CancellationToken cancellationToken);
}

public interface IActivateRoomUseCase
{
    Task ExecuteAsync(string roomCode, CancellationToken cancellationToken);
}

public interface IRetireRoomUseCase
{
    Task ExecuteAsync(string roomCode, CancellationToken cancellationToken);
}

public interface IUpdateWorkforceMemberDepartmentUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string departmentCode,
        CancellationToken cancellationToken);
}

public interface IUpdateWorkforceMemberWorkforceTypeUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string workforceType,
        CancellationToken cancellationToken);
}

public interface IUpdateRoomNumberUseCase
{
    Task ExecuteAsync(string roomCode, string roomNumber, CancellationToken cancellationToken);
}

public interface IUpdateRoomDescriptionUseCase
{
    Task ExecuteAsync(string roomCode, string? description, CancellationToken cancellationToken);
}

public interface IUpdatePartyNameUseCase
{
    Task ExecuteAsync(
        string partyCode,
        string firstName,
        string lastName,
        CancellationToken cancellationToken);
}

public interface ICorrectPartyUinUseCase
{
    Task ExecuteAsync(string partyCode, string newUin, CancellationToken cancellationToken);
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

public sealed class ActivateDepartmentUseCase : IActivateDepartmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public ActivateDepartmentUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string departmentCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        Department? department = await _workforce.FindDepartmentAsync(departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        department.Activate();
        _audit.Stage(
            OperatorAuditActions.DepartmentActivated,
            OperatorAuditSubjects.Department,
            department.DepartmentCode);
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

    public async Task ExecuteAsync(string departmentCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        Department? department = await _workforce
            .FindDepartmentAsync(departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null)
        {
            throw new InvalidOperationException("The department was not found.");
        }

        department.Retire();
        _audit.Stage(
            OperatorAuditActions.DepartmentRetired,
            OperatorAuditSubjects.Department,
            department.DepartmentCode);
        await _workforce.UpdateDepartmentAsync(department, cancellationToken).ConfigureAwait(false);
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

        room.Activate();
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

public sealed class UpdateWorkforceMemberDepartmentUseCase : IUpdateWorkforceMemberDepartmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateWorkforceMemberDepartmentUseCase(
        IWorkforcePersistencePort workforce,
        IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);

        WorkforceMember member = await RequireActiveMemberAsync(workforceMemberCode.Trim(), cancellationToken)
            .ConfigureAwait(false);

        Department? department = await _workforce.FindDepartmentAsync(departmentCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException("Department must exist and be active.");
        }

        member.AssignDepartment(department.DepartmentCode);
        _audit.Stage(
            OperatorAuditActions.WorkforceMemberMaintained,
            OperatorAuditSubjects.WorkforceMember,
            member.WorkforceMemberCode,
            $"Department={department.DepartmentCode}");
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
            throw new InvalidOperationException("Only an Active WorkforceMember may change Department.");
        }

        return member;
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

public sealed class UpdateRoomNumberUseCase : IUpdateRoomNumberUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateRoomNumberUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string roomCode, string roomNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomNumber);

        Room? room = await _workforce.FindRoomAsync(roomCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        string trimmedNumber = roomNumber.Trim();
        if (!string.Equals(room.RoomNumber, trimmedNumber, StringComparison.Ordinal)
            && await _workforce.RoomNumberExistsAsync(trimmedNumber, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("RoomNumber must be globally unique.");
        }

        string oldRoomNumber = room.RoomNumber;
        room.UpdateRoomNumber(trimmedNumber);
        _audit.Stage(
            OperatorAuditActions.RoomUpdated,
            OperatorAuditSubjects.Room,
            room.RoomCode,
            $"RoomNumber={oldRoomNumber}→{room.RoomNumber}");
        await _workforce.UpdateRoomAsync(room, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpdateRoomDescriptionUseCase : IUpdateRoomDescriptionUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdateRoomDescriptionUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string roomCode, string? description, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        Room? room = await _workforce.FindRoomAsync(roomCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        string oldDescription = room.Description;
        room.UpdateDescription(description);
        _audit.Stage(
            OperatorAuditActions.RoomUpdated,
            OperatorAuditSubjects.Room,
            room.RoomCode,
            $"Description={oldDescription}→{room.Description}");
        await _workforce.UpdateRoomAsync(room, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class UpdatePartyNameUseCase : IUpdatePartyNameUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public UpdatePartyNameUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string partyCode,
        string firstName,
        string lastName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partyCode);

        Party? party = await _workforce.FindPartyAsync(partyCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (party is null)
        {
            throw new InvalidOperationException("The party was not found.");
        }

        string oldFirstName = party.FirstName;
        string oldLastName = party.LastName;
        party.Rename(firstName, lastName);
        _audit.Stage(
            OperatorAuditActions.PartyNameUpdated,
            OperatorAuditSubjects.Party,
            party.PartyCode,
            $"FirstName={oldFirstName}→{party.FirstName}; LastName={oldLastName}→{party.LastName}");
        await _workforce.UpdatePartyAsync(party, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class CorrectPartyUinUseCase : ICorrectPartyUinUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CorrectPartyUinUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string partyCode, string newUin, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(newUin);

        Party? party = await _workforce.FindPartyAsync(partyCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (party is null)
        {
            throw new InvalidOperationException("The party was not found.");
        }

        Party? existingWithUin = await _workforce.FindPartyByUinAsync(newUin.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (existingWithUin is not null
            && !string.Equals(existingWithUin.PartyCode, party.PartyCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A party with this UIN already exists.");
        }

        string oldUin = party.Uin;
        party.CorrectUin(newUin);
        _audit.Stage(
            OperatorAuditActions.PartyUinCorrected,
            OperatorAuditSubjects.Party,
            party.PartyCode,
            $"UIN={oldUin}→{party.Uin}");
        await _workforce.UpdatePartyAsync(party, cancellationToken).ConfigureAwait(false);
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
