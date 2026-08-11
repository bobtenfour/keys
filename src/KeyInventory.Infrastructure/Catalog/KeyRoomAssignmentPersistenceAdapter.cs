using KeyInventory.Application.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Catalog;

public sealed class KeyRoomAssignmentPersistenceAdapter : IKeyRoomAssignmentPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;

    public KeyRoomAssignmentPersistenceAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AssignAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogKeyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        bool exists = await ExistsAsync(catalogKeyCode, roomCode, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException("A current Key-to-Room assignment for this Key and Room already exists.");
        }

        _dbContext.KeyRoomAssignments.Add(new KeyRoomAssignmentEntity
        {
            CatalogKeyCode = catalogKeyCode.Trim(),
            RoomCode = roomCode.Trim()
        });
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogKeyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);

        KeyRoomAssignmentEntity? entity = await _dbContext.KeyRoomAssignments
            .FirstOrDefaultAsync(
                item => item.CatalogKeyCode == catalogKeyCode.Trim() && item.RoomCode == roomCode.Trim(),
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException("The Key-to-Room assignment was not found.");
        }

        _dbContext.KeyRoomAssignments.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(string catalogKeyCode, string roomCode, CancellationToken cancellationToken)
    {
        return _dbContext.KeyRoomAssignments.AnyAsync(
            item => item.CatalogKeyCode == catalogKeyCode.Trim() && item.RoomCode == roomCode.Trim(),
            cancellationToken);
    }

    public Task<bool> HasAnyAssignmentAsync(CancellationToken cancellationToken)
    {
        return _dbContext.KeyRoomAssignments.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KeyOpenedRoomItem>> ListForKeyAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogKeyCode);
        List<KeyOpenedRoomItem> rooms = await (
                from assignment in _dbContext.KeyRoomAssignments.AsNoTracking()
                join room in _dbContext.Rooms.AsNoTracking() on assignment.RoomCode equals room.RoomCode
                where assignment.CatalogKeyCode == catalogKeyCode.Trim()
                orderby room.RoomNumber
                select new KeyOpenedRoomItem(room.RoomCode, room.RoomNumber, room.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rooms;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ListForKeysAsync(
        IEnumerable<string> catalogKeyCodes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalogKeyCodes);
        HashSet<string> codes = catalogKeyCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (codes.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<KeyOpenedRoomItem>>(StringComparer.Ordinal);
        }

        List<KeyOpenedRoomItemRow> rows = await (
                from assignment in _dbContext.KeyRoomAssignments.AsNoTracking()
                join room in _dbContext.Rooms.AsNoTracking() on assignment.RoomCode equals room.RoomCode
                where codes.Contains(assignment.CatalogKeyCode)
                orderby assignment.CatalogKeyCode, room.RoomNumber
                select new KeyOpenedRoomItemRow(
                    assignment.CatalogKeyCode,
                    room.RoomCode,
                    room.RoomNumber,
                    room.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, IReadOnlyList<KeyOpenedRoomItem>> map = codes.ToDictionary(
            code => code,
            _ => (IReadOnlyList<KeyOpenedRoomItem>)[],
            StringComparer.Ordinal);

        foreach (IGrouping<string, KeyOpenedRoomItemRow> group in rows.GroupBy(
                     row => row.CatalogKeyCode,
                     StringComparer.Ordinal))
        {
            map[group.Key] = group
                .Select(row => new KeyOpenedRoomItem(row.RoomCode, row.RoomNumber, row.Description))
                .ToArray();
        }

        return map;
    }

    private sealed record KeyOpenedRoomItemRow(
        string CatalogKeyCode,
        string RoomCode,
        string RoomNumber,
        string Description);
}
