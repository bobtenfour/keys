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

    public CreateWorkAssignmentUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
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

        Building? building = await _workforce.FindBuildingAsync(room.BuildingCode, cancellationToken)
            .ConfigureAwait(false);
        if (building is null || !building.IsActive)
        {
            throw new InvalidOperationException("WorkAssignment requires an active Building for the Room.");
        }

        if (isPrimary)
        {
            await _workforce.ClearPrimaryAssignmentsAsync(member.WorkforceMemberCode, cancellationToken)
                .ConfigureAwait(false);
        }

        WorkAssignment assignment = new(workAssignmentCode, member.WorkforceMemberCode, room.RoomCode, isPrimary);
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
