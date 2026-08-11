using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface ICreateWorkAssignmentUseCase
{
    Task ExecuteAsync(
        string workAssignmentCode,
        string workforceMemberCode,
        string roomCode,
        bool isPrimary,
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
        string workAssignmentCode,
        string workforceMemberCode,
        string roomCode,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        if (await _workforce.WorkAssignmentExistsAsync(workAssignmentCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A work assignment with this code already exists.");
        }

        WorkforceMember? member = await _workforce.FindWorkforceMemberAsync(workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null || member.Status != WorkforceMemberStatus.Active)
        {
            throw new InvalidOperationException("WorkAssignment requires an Active WorkforceMember.");
        }

        Room? room = await _workforce.FindRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);
        if (room is null || !room.IsActive)
        {
            throw new InvalidOperationException("WorkAssignment requires an active Room.");
        }

        if (isPrimary)
        {
            await _workforce.ClearPrimaryAssignmentsAsync(member.WorkforceMemberCode, cancellationToken)
                .ConfigureAwait(false);
        }

        WorkAssignment assignment = new(workAssignmentCode, member.WorkforceMemberCode, room.RoomCode, isPrimary);
        _audit.Stage(
            OperatorAuditActions.WorkAssignmentCreated,
            OperatorAuditSubjects.WorkAssignment,
            assignment.WorkAssignmentCode,
            $"WorkforceMember={assignment.WorkforceMemberCode}; Room={assignment.RoomCode}; Primary={assignment.IsPrimary}");
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
