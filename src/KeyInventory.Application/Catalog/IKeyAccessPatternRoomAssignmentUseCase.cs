namespace KeyInventory.Application.Catalog;

public interface IKeyAccessPatternRoomAssignmentUseCase
{
    Task AssignRoomAsync(string keyNumber, string roomCode, CancellationToken cancellationToken);

    Task RemoveRoomAsync(string keyNumber, string roomCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyOpenedRoomItem>> ListOpenedRoomsAsync(
        string keyNumber,
        CancellationToken cancellationToken);
}
