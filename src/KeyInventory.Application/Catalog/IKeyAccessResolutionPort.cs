namespace KeyInventory.Application.Catalog;

/// <summary>
/// Resolves KEY # access for display and expansion.
/// Regular: single Room from pattern RoomCode.
/// Master: expands to all active Rooms when expansion is required; display uses "Access: All Rooms".
/// </summary>
public interface IKeyAccessResolutionPort
{
    /// <summary>
    /// Resolves opened rooms for a KEY #. When <paramref name="expandMaster"/> is false,
    /// Master returns an empty list (display formatter shows "Access: All Rooms").
    /// When true, Master returns all active Rooms.
    /// </summary>
    Task<IReadOnlyList<KeyOpenedRoomItem>> ResolveForKeyNumberAsync(
        string keyNumber,
        bool expandMaster,
        CancellationToken cancellationToken);

    /// <summary>
    /// Batch resolve for many KEY # values using classification + RoomCode already known.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ResolveForPatternsAsync(
        IEnumerable<KeyAccessResolutionRequest> patterns,
        bool expandMaster,
        CancellationToken cancellationToken);

    /// <summary>
    /// KEY # values that open the given Room: Regular where RoomCode matches, plus all Master KEY #s.
    /// </summary>
    Task<IReadOnlyList<string>> ListKeyNumbersOpeningRoomAsync(
        string roomCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// True when at least one KEY # has valid access (Regular with RoomCode, or any Master).
    /// </summary>
    Task<bool> HasValidKeyAccessAsync(CancellationToken cancellationToken);

    /// <summary>
    /// True when any Regular KEY # stores this RoomCode.
    /// </summary>
    Task<bool> RegularKeyReferencesRoomAsync(string roomCode, CancellationToken cancellationToken);
}

public sealed record KeyAccessResolutionRequest(
    string KeyNumber,
    KeyInventory.Domain.Catalog.KeyAccessClassification Classification,
    string? RoomCode);
