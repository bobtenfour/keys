using KeyInventory.Application.Catalog;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Catalog;

public sealed class KeyAccessResolutionAdapter : IKeyAccessResolutionPort
{
    private readonly KeyInventoryDbContext _dbContext;

    public KeyAccessResolutionAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<KeyOpenedRoomItem>> ResolveForKeyNumberAsync(
        string keyNumber,
        bool expandMaster,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        KeyAccessPatternEntity? pattern = await _dbContext.KeyAccessPatterns.AsNoTracking()
            .FirstOrDefaultAsync(item => item.KeyNumber == keyNumber.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (pattern is null)
        {
            return [];
        }

        KeyAccessClassification classification =
            Workflow.DomainCatalogMapper.ParseClassification(pattern.Classification);
        return await ResolveOneAsync(classification, pattern.RoomCode, expandMaster, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ResolveForPatternsAsync(
        IEnumerable<KeyAccessResolutionRequest> patterns,
        bool expandMaster,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        List<KeyAccessResolutionRequest> list = patterns
            .Where(item => !string.IsNullOrWhiteSpace(item.KeyNumber))
            .Select(item => item with { KeyNumber = item.KeyNumber.Trim() })
            .GroupBy(item => item.KeyNumber, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        Dictionary<string, IReadOnlyList<KeyOpenedRoomItem>> map = new(StringComparer.Ordinal);
        if (list.Count == 0)
        {
            return map;
        }

        IReadOnlyList<KeyOpenedRoomItem>? allActiveRooms = null;
        if (expandMaster && list.Any(item => item.Classification == KeyAccessClassification.Master))
        {
            allActiveRooms = await ListActiveRoomsAsync(cancellationToken).ConfigureAwait(false);
        }

        HashSet<string> regularRoomCodes = list
            .Where(item => item.Classification == KeyAccessClassification.Regular
                && !string.IsNullOrWhiteSpace(item.RoomCode))
            .Select(item => item.RoomCode!.Trim())
            .ToHashSet(StringComparer.Ordinal);

        Dictionary<string, KeyOpenedRoomItem> roomsByCode = new(StringComparer.Ordinal);
        if (regularRoomCodes.Count > 0)
        {
            List<KeyOpenedRoomItem> rooms = await _dbContext.Rooms.AsNoTracking()
                .Where(room => regularRoomCodes.Contains(room.RoomCode))
                .Select(room => new KeyOpenedRoomItem(room.RoomCode, room.RoomNumber, room.Description))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (KeyOpenedRoomItem room in rooms)
            {
                roomsByCode[room.RoomCode] = room;
            }
        }

        foreach (KeyAccessResolutionRequest request in list)
        {
            if (request.Classification == KeyAccessClassification.Master)
            {
                map[request.KeyNumber] = expandMaster
                    ? allActiveRooms ?? []
                    : [];
                continue;
            }

            if (string.IsNullOrWhiteSpace(request.RoomCode)
                || !roomsByCode.TryGetValue(request.RoomCode.Trim(), out KeyOpenedRoomItem? room))
            {
                map[request.KeyNumber] = [];
                continue;
            }

            map[request.KeyNumber] = [room];
        }

        return map;
    }

    public async Task<IReadOnlyList<string>> ListKeyNumbersOpeningRoomAsync(
        string roomCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        string normalized = roomCode.Trim();

        return await _dbContext.KeyAccessPatterns.AsNoTracking()
            .Where(pattern =>
                pattern.Classification == nameof(KeyAccessClassification.Master)
                || (pattern.Classification == nameof(KeyAccessClassification.Regular)
                    && pattern.RoomCode == normalized))
            .OrderBy(pattern => pattern.KeyNumber)
            .Select(pattern => pattern.KeyNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> HasValidKeyAccessAsync(CancellationToken cancellationToken)
    {
        return _dbContext.KeyAccessPatterns.AsNoTracking().AnyAsync(
            pattern =>
                pattern.Classification == nameof(KeyAccessClassification.Master)
                || (pattern.Classification == nameof(KeyAccessClassification.Regular)
                    && pattern.RoomCode != null),
            cancellationToken);
    }

    public Task<bool> RegularKeyReferencesRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomCode);
        return _dbContext.KeyAccessPatterns.AsNoTracking().AnyAsync(
            pattern => pattern.Classification == nameof(KeyAccessClassification.Regular)
                && pattern.RoomCode == roomCode.Trim(),
            cancellationToken);
    }

    private async Task<IReadOnlyList<KeyOpenedRoomItem>> ResolveOneAsync(
        KeyAccessClassification classification,
        string? roomCode,
        bool expandMaster,
        CancellationToken cancellationToken)
    {
        if (classification == KeyAccessClassification.Master)
        {
            return expandMaster
                ? await ListActiveRoomsAsync(cancellationToken).ConfigureAwait(false)
                : [];
        }

        if (string.IsNullOrWhiteSpace(roomCode))
        {
            return [];
        }

        KeyOpenedRoomItem? room = await _dbContext.Rooms.AsNoTracking()
            .Where(item => item.RoomCode == roomCode.Trim())
            .Select(item => new KeyOpenedRoomItem(item.RoomCode, item.RoomNumber, item.Description))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return room is null ? [] : [room];
    }

    private async Task<IReadOnlyList<KeyOpenedRoomItem>> ListActiveRoomsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Rooms.AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.RoomNumber)
            .Select(room => new KeyOpenedRoomItem(room.RoomCode, room.RoomNumber, room.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
