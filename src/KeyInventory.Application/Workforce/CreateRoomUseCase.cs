using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;

namespace KeyInventory.Application.Workforce;

public interface ICreateRoomUseCase
{
    Task ExecuteAsync(
        string roomCode,
        string buildingCode,
        string roomNumber,
        string? description,
        CancellationToken cancellationToken);
}

public interface IListRoomsUseCase
{
    Task<IReadOnlyList<RoomListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreateRoomUseCase : ICreateRoomUseCase
{
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CreateRoomUseCase(IWorkforcePersistencePort workforce, IOperatorAuditRecorder audit)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(
        string roomCode,
        string buildingCode,
        string roomNumber,
        string? description,
        CancellationToken cancellationToken)
    {
        if (await _workforce.RoomExistsAsync(roomCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A room with this room code already exists.");
        }

        Building? building = await _workforce.FindBuildingAsync(buildingCode, cancellationToken).ConfigureAwait(false);
        if (building is null)
        {
            throw new InvalidOperationException("The building was not found.");
        }

        if (!building.IsActive)
        {
            throw new InvalidOperationException("Room cannot reference an inactive Building.");
        }

        Room room = new(roomCode, building, roomNumber, description);
        if (await _workforce.RoomNumberExistsInBuildingAsync(building.BuildingCode, room.RoomNumber, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("RoomNumber must be unique within the Building.");
        }

        _audit.Stage(
            OperatorAuditActions.RoomCreated,
            OperatorAuditSubjects.Room,
            room.RoomCode,
            $"Building={building.BuildingCode}; RoomNumber={room.RoomNumber}");
        await _workforce.AddRoomAsync(room, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListRoomsUseCase : IListRoomsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListRoomsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<RoomListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListRoomsAsync(cancellationToken);
    }
}
