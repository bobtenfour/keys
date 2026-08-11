using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Locations;

namespace KeyInventory.Application.Workforce;

public interface ICreateRoomUseCase
{
    /// <summary>
    /// Creates a Room with a system-generated RoomCode. Returns the generated RoomCode.
    /// </summary>
    Task<string> ExecuteAsync(
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

    public async Task<string> ExecuteAsync(
        string roomNumber,
        string? description,
        CancellationToken cancellationToken)
    {
        string roomCode = WorkforceIdentityCodes.NewRoomCode();
        Room room = new(roomCode, roomNumber, description);

        if (await _workforce.RoomNumberExistsAsync(room.RoomNumber, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("RoomNumber must be globally unique.");
        }

        _audit.Stage(
            OperatorAuditActions.RoomCreated,
            OperatorAuditSubjects.Room,
            room.RoomCode,
            $"RoomNumber={room.RoomNumber}");
        await _workforce.AddRoomAsync(room, cancellationToken).ConfigureAwait(false);
        return roomCode;
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
