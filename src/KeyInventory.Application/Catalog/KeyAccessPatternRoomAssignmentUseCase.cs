using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workflow;
using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Locations;

namespace KeyInventory.Application.Catalog;

public sealed class KeyAccessPatternRoomAssignmentUseCase : IKeyAccessPatternRoomAssignmentUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IKeyAccessPatternRoomAssignmentPersistencePort _assignments;
    private readonly IOperatorAuditRecorder _audit;

    public KeyAccessPatternRoomAssignmentUseCase(
        IKeyCatalogPersistencePort catalog,
        IWorkforcePersistencePort workforce,
        IKeyAccessPatternRoomAssignmentPersistencePort assignments,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task AssignRoomAsync(string keyNumber, string roomCode, CancellationToken cancellationToken)
    {
        KeyAccessPattern pattern = await RequirePatternAsync(keyNumber, cancellationToken).ConfigureAwait(false);
        Room room = await RequireRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);

        pattern.AssignOpenedRoom(room.RoomCode);
        _audit.Stage(
            OperatorAuditActions.KeyAccessPatternRoomAssignmentAdded,
            OperatorAuditSubjects.KeyAccessPatternRoomAssignment,
            $"{pattern.KeyNumber}@{room.RoomCode}",
            $"KEY#={pattern.KeyNumber}; Room={room.RoomCode}; RoomNumber={room.RoomNumber}");
        await _assignments.AssignAsync(pattern.KeyNumber, room.RoomCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveRoomAsync(string keyNumber, string roomCode, CancellationToken cancellationToken)
    {
        KeyAccessPattern pattern = await RequirePatternAsync(keyNumber, cancellationToken).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        string normalizedRoomCode = roomCode.Trim();
        pattern.RemoveOpenedRoom(normalizedRoomCode);
        _audit.Stage(
            OperatorAuditActions.KeyAccessPatternRoomAssignmentRemoved,
            OperatorAuditSubjects.KeyAccessPatternRoomAssignment,
            $"{pattern.KeyNumber}@{normalizedRoomCode}",
            $"KEY#={pattern.KeyNumber}; Room={normalizedRoomCode}");
        await _assignments.RemoveAsync(pattern.KeyNumber, normalizedRoomCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<IReadOnlyList<KeyOpenedRoomItem>> ListOpenedRoomsAsync(
        string keyNumber,
        CancellationToken cancellationToken)
        => _assignments.ListForKeyNumberAsync(keyNumber, cancellationToken);

    private async Task<KeyAccessPattern> RequirePatternAsync(string keyNumber, CancellationToken cancellationToken)
    {
        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            throw new InvalidOperationException("The KEY # was not found.");
        }

        return pattern;
    }

    private async Task<Room> RequireRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        Room? room = await _workforce.FindRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);
        if (room is null)
        {
            throw new InvalidOperationException("The room was not found.");
        }

        if (!room.IsActive)
        {
            throw new InvalidOperationException("An inactive room cannot be assigned to a KEY #.");
        }

        return room;
    }
}
