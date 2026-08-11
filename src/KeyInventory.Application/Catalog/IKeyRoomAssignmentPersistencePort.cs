namespace KeyInventory.Application.Catalog;

public interface IKeyRoomAssignmentPersistencePort
{
    Task AssignAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken);

    Task RemoveAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken);

    Task<bool> HasAnyAssignmentAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyOpenedRoomItem>> ListForKeyAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ListForKeysAsync(
        IEnumerable<string> catalogKeyCodes,
        CancellationToken cancellationToken);
}
