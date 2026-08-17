using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateWorkAssignmentUseCase
{
    Task ExecuteAsync(
        string workforceMemberCode,
        string roomCode,
        CancellationToken cancellationToken);
}

public interface IListWorkAssignmentsUseCase
{
    Task<IReadOnlyList<WorkAssignmentListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreateWorkAssignmentUseCase : ICreateWorkAssignmentUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CreateWorkAssignmentUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string workforceMemberCode,
        string roomCode,
        CancellationToken cancellationToken)
    {
        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null || member.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException(
                "Room assignment requires an active workforce member.");
        }

        Room? room = await _workforce.FindRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);
        if (room is null || !room.IsActive)
        {
            throw new InvalidOperationException("Room assignment requires an active Room.");
        }

        if (room.DepartmentId != member.DepartmentId)
        {
            throw new InvalidOperationException(
                "Room assignment rejected: the Room's Department does not match the workforce member's Department. Cross-department room assignments are not allowed.");
        }

        if (await _workforce
                .ActiveWorkAssignmentExistsAsync(member.WorkforceMemberCode, room.RoomCode, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "An active room assignment already exists for this workforce member and Room.");
        }

        WorkAssignment assignment = new(Guid.NewGuid(), member.WorkforceMemberCode, room.RoomCode);
        _audit.Stage(
            OperatorAuditActions.WorkAssignmentCreated,
            OperatorAuditSubjects.WorkAssignment,
            assignment.WorkAssignmentId.ToString("D"),
            $"WorkforceMember={assignment.WorkforceMemberCode}; Room={assignment.RoomCode}");
        await _workforce.AddWorkAssignmentAsync(assignment, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListWorkAssignmentsUseCase : IListWorkAssignmentsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListWorkAssignmentsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<WorkAssignmentListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListWorkAssignmentsAsync(cancellationToken);
    }
}
