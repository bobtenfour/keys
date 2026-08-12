namespace KeyInventory.Application.Catalog;

public interface IKeyAccessPatternRoomAssignmentPersistencePort
{
    Task AssignAsync(string keyNumber, string roomCode, CancellationToken cancellationToken);

    Task RemoveAsync(string keyNumber, string roomCode, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string keyNumber, string roomCode, CancellationToken cancellationToken);

    Task<bool> HasAnyAssignmentAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyOpenedRoomItem>> ListForKeyNumberAsync(
        string keyNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ListForKeyNumbersAsync(
        IEnumerable<string> keyNumbers,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListKeyNumbersForRoomAsync(
        string roomCode,
        CancellationToken cancellationToken);
}
