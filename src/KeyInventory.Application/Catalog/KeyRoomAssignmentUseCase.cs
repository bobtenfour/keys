using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Locations;

namespace KeyInventory.Application.Catalog;

public sealed class KeyRoomAssignmentUseCase : IKeyRoomAssignmentUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IKeyRoomAssignmentPersistencePort _assignments;
    private readonly IOperatorAuditRecorder _audit;

    public KeyRoomAssignmentUseCase(
        IKeyCatalogPersistencePort catalog,
        IWorkforcePersistencePort workforce,
        IKeyRoomAssignmentPersistencePort assignments,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task AssignRoomAsync(
        string catalogKeyCode,
        string roomCode,
        CancellationToken cancellationToken)
    {
        KeyAsset keyAsset = await RequireKeyAssetAsync(catalogKeyCode, cancellationToken).ConfigureAwait(false);
        Room room = await RequireActiveRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);

        keyAsset.AssignOpenedRoom(room.RoomCode);
        _audit.Stage(
            OperatorAuditActions.KeyRoomAssignmentAdded,
            OperatorAuditSubjects.KeyRoomAssignment,
            $"{keyAsset.CatalogKeyCode}@{room.RoomCode}",
            $"Key={keyAsset.CatalogKeyCode}; Room={room.RoomCode}");
        await _assignments.AssignAsync(keyAsset.CatalogKeyCode, room.RoomCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveRoomAsync(
        string catalogKeyCode,
        string roomCode,
        CancellationToken cancellationToken)
    {
        KeyAsset keyAsset = await RequireKeyAssetAsync(catalogKeyCode, cancellationToken).ConfigureAwait(false);
        string normalizedRoomCode = roomCode?.Trim() ?? string.Empty;
        keyAsset.RemoveOpenedRoom(normalizedRoomCode);
        _audit.Stage(
            OperatorAuditActions.KeyRoomAssignmentRemoved,
            OperatorAuditSubjects.KeyRoomAssignment,
            $"{keyAsset.CatalogKeyCode}@{normalizedRoomCode}",
            $"Key={keyAsset.CatalogKeyCode}; Room={normalizedRoomCode}");
        await _assignments.RemoveAsync(keyAsset.CatalogKeyCode, normalizedRoomCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<IReadOnlyList<KeyOpenedRoomItem>> ListOpenedRoomsAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogKeyCode);
        return _assignments.ListForKeyAsync(catalogKeyCode.Trim(), cancellationToken);
    }

    private async Task<KeyAsset> RequireKeyAssetAsync(string catalogKeyCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogKeyCode);
        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(catalogKeyCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The key was not found in the catalog.");
        }

        return keyAsset;
    }

    private async Task<Room> RequireActiveRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        Room? room = await _workforce.FindRoomAsync(roomCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        if (!room.IsActive)
        {
            throw new InvalidOperationException("An inactive room cannot be assigned to a key.");
        }

        return room;
    }
}
