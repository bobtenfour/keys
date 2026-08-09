namespace KeyInventory.Application.Catalog;

public interface IKeyRoomAssignmentUseCase
{
    Task AssignRoomAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken);

    Task RemoveRoomAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyOpenedRoomItem>> ListOpenedRoomsAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken);
}
