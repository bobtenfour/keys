using KeyInventory.Application.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Catalog;

public sealed class KeyAccessPatternRoomAssignmentPersistenceAdapter
    : IKeyAccessPatternRoomAssignmentPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;

    public KeyAccessPatternRoomAssignmentPersistenceAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AssignAsync(string keyNumber, string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        bool exists = await ExistsAsync(keyNumber, roomCode, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException("A current KEY # to Room assignment for this KEY # and Room already exists.");
        }

        _dbContext.KeyAccessPatternRoomAssignments.Add(new KeyAccessPatternRoomAssignmentEntity
        {
            KeyNumber = keyNumber.Trim(),
            RoomCode = roomCode.Trim()
        });
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string keyNumber, string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        KeyAccessPatternRoomAssignmentEntity? entity = await _dbContext.KeyAccessPatternRoomAssignments
            .FirstOrDefaultAsync(
                item => item.KeyNumber == keyNumber.Trim() && item.RoomCode == roomCode.Trim(),
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException("The KEY # to Room assignment was not found.");
        }

        _dbContext.KeyAccessPatternRoomAssignments.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(string keyNumber, string roomCode, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAccessPatternRoomAssignments.AnyAsync(
            item => item.KeyNumber == keyNumber.Trim() && item.RoomCode == roomCode.Trim(),
            cancellationToken);
    }

    public Task<bool> HasAnyAssignmentAsync(CancellationToken cancellationToken)
    {
        return _dbContext.KeyAccessPatternRoomAssignments.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KeyOpenedRoomItem>> ListForKeyNumberAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        List<KeyOpenedRoomItem> rooms = await (
                from assignment in _dbContext.KeyAccessPatternRoomAssignments.AsNoTracking()
                join room in _dbContext.Rooms.AsNoTracking() on assignment.RoomCode equals room.RoomCode
                where assignment.KeyNumber == keyNumber.Trim()
                orderby room.RoomNumber
                select new KeyOpenedRoomItem(room.RoomCode, room.RoomNumber, room.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rooms;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ListForKeyNumbersAsync(
        IEnumerable<string> keyNumbers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keyNumbers);
        HashSet<string> numbers = keyNumbers
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (numbers.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<KeyOpenedRoomItem>>(StringComparer.Ordinal);
        }

        List<KeyOpenedRoomItemRow> rows = await (
                from assignment in _dbContext.KeyAccessPatternRoomAssignments.AsNoTracking()
                join room in _dbContext.Rooms.AsNoTracking() on assignment.RoomCode equals room.RoomCode
                where numbers.Contains(assignment.KeyNumber)
                orderby assignment.KeyNumber, room.RoomNumber
                select new KeyOpenedRoomItemRow(
                    assignment.KeyNumber,
                    room.RoomCode,
                    room.RoomNumber,
                    room.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, IReadOnlyList<KeyOpenedRoomItem>> map = numbers.ToDictionary(
            code => code,
            _ => (IReadOnlyList<KeyOpenedRoomItem>)[],
            StringComparer.Ordinal);

        foreach (IGrouping<string, KeyOpenedRoomItemRow> group in rows.GroupBy(
                     row => row.KeyNumber,
                     StringComparer.Ordinal))
        {
            map[group.Key] = group
                .Select(row => new KeyOpenedRoomItem(row.RoomCode, row.RoomNumber, row.Description))
                .ToArray();
        }

        return map;
    }

    public async Task<IReadOnlyList<string>> ListKeyNumbersForRoomAsync(
        string roomCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        return await _dbContext.KeyAccessPatternRoomAssignments.AsNoTracking()
            .Where(item => item.RoomCode == roomCode.Trim())
            .OrderBy(item => item.KeyNumber)
            .Select(item => item.KeyNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed record KeyOpenedRoomItemRow(
        string KeyNumber,
        string RoomCode,
        string RoomNumber,
        string Description);
}
