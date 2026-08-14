using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Workflow;

public sealed class KeyCatalogPersistenceAdapter : IKeyCatalogPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyAccessPatternRoomAssignmentPersistencePort _roomAssignments;

    public KeyCatalogPersistenceAdapter(
        KeyInventoryDbContext dbContext,
        IKeyAccessPatternRoomAssignmentPersistencePort roomAssignments)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _roomAssignments = roomAssignments ?? throw new ArgumentNullException(nameof(roomAssignments));
    }

    public Task<bool> KeyAccessPatternExistsAsync(string keyNumber, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAccessPatterns.AnyAsync(
            entity => entity.KeyNumber == keyNumber,
            cancellationToken);
    }

    public async Task<KeyAccessPattern?> FindKeyAccessPatternAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        KeyAccessPatternEntity? entity = await _dbContext.KeyAccessPatterns
            .AsNoTracking()
            .Include(item => item.KeyType)
            .FirstOrDefaultAsync(item => item.KeyNumber == keyNumber, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        IReadOnlyList<KeyOpenedRoomItem> rooms = await _roomAssignments
            .ListForKeyNumberAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);
        return DomainCatalogMapper.ToDomain(entity, rooms.Select(room => room.RoomCode));
    }

    public async Task AddKeyAccessPatternAsync(KeyAccessPattern pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        _dbContext.KeyAccessPatterns.Add(DomainCatalogMapper.ToEntity(pattern));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateKeyAccessPatternAsync(KeyAccessPattern pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        KeyAccessPatternEntity? entity = await _dbContext.KeyAccessPatterns
            .FirstOrDefaultAsync(item => item.KeyNumber == pattern.KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The KEY # was not found in persistence.");
        }

        entity.KeyTypeCode = pattern.KeyType.TypeCode;
        entity.IsActive = pattern.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<KeyAccessPatternListItem>> ListKeyAccessPatternsAsync(
        CancellationToken cancellationToken)
    {
        List<KeyAccessPatternEntity> patterns = await _dbContext.KeyAccessPatterns
            .AsNoTracking()
            .OrderBy(entity => entity.KeyNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, int> copyCounts = await _dbContext.KeyAssets.AsNoTracking()
            .GroupBy(entity => entity.KeyNumber)
            .Select(group => new { KeyNumber = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.KeyNumber, item => item.Count, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey = await _roomAssignments
            .ListForKeyNumbersAsync(patterns.Select(item => item.KeyNumber), cancellationToken)
            .ConfigureAwait(false);

        return patterns
            .Select(entity => new KeyAccessPatternListItem(
                entity.KeyNumber,
                entity.KeyTypeCode,
                entity.IsActive,
                copyCounts.TryGetValue(entity.KeyNumber, out int count) ? count : 0,
                roomsByKey.TryGetValue(entity.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : []))
            .ToArray();
    }

    public async Task<IReadOnlyList<KeyAccessPatternListItem>> SearchActiveKeyAccessPatternsAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText) || maxResults < 1)
        {
            return [];
        }

        string term = searchText.Trim();
        int bound = Math.Min(maxResults, 25);

        List<KeyAccessPatternEntity> patterns = await _dbContext.KeyAccessPatterns
            .AsNoTracking()
            .Where(entity => entity.IsActive && entity.KeyNumber.Contains(term))
            .OrderBy(entity => entity.KeyNumber)
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (patterns.Count == 0)
        {
            return [];
        }

        Dictionary<string, int> copyCounts = await _dbContext.KeyAssets.AsNoTracking()
            .Where(entity => patterns.Select(pattern => pattern.KeyNumber).Contains(entity.KeyNumber))
            .GroupBy(entity => entity.KeyNumber)
            .Select(group => new { KeyNumber = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.KeyNumber, item => item.Count, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey = await _roomAssignments
            .ListForKeyNumbersAsync(patterns.Select(item => item.KeyNumber), cancellationToken)
            .ConfigureAwait(false);

        return patterns
            .Select(entity => new KeyAccessPatternListItem(
                entity.KeyNumber,
                entity.KeyTypeCode,
                entity.IsActive,
                copyCounts.TryGetValue(entity.KeyNumber, out int count) ? count : 0,
                roomsByKey.TryGetValue(entity.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : []))
            .ToArray();
    }

    public Task<bool> MedecoExistsUnderPatternAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.KeyAssets.AnyAsync(
            entity => entity.KeyNumber == keyNumber && entity.MedecoKeyCode == medecoKeyCode,
            cancellationToken);
    }

    public async Task<KeyType?> FindKeyTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        KeyTypeEntity? entity = await _dbContext.KeyTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TypeCode == typeCode, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : DomainCatalogMapper.ToDomain(entity);
    }

    public async Task AddKeyTypeAsync(KeyType keyType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keyType);
        _dbContext.KeyTypes.Add(DomainCatalogMapper.ToEntity(keyType));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateKeyTypeAsync(KeyType keyType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keyType);
        KeyTypeEntity? entity = await _dbContext.KeyTypes
            .FirstOrDefaultAsync(item => item.TypeCode == keyType.TypeCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The key type was not found in persistence.");
        }

        entity.IsActive = keyType.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteKeyTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        KeyTypeEntity? entity = await _dbContext.KeyTypes
            .FirstOrDefaultAsync(item => item.TypeCode == typeCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The key type was not found in persistence.");
        }

        _dbContext.KeyTypes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountActiveKeyAccessPatternsForTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAccessPatterns.CountAsync(
            entity => entity.KeyTypeCode == typeCode && entity.IsActive,
            cancellationToken);
    }

    public Task<int> CountKeyAccessPatternsForTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        return CountAllKeyAccessPatternsForTypeAsync(typeCode, cancellationToken);
    }

    public Task<int> CountAllKeyAccessPatternsForTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAccessPatterns.CountAsync(
            entity => entity.KeyTypeCode == typeCode,
            cancellationToken);
    }

    public async Task<IReadOnlyList<KeyTypeListItem>> ListKeyTypesAsync(CancellationToken cancellationToken)
    {
        List<KeyTypeEntity> types = await _dbContext.KeyTypes.AsNoTracking()
            .OrderBy(entity => entity.TypeCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, int> activeCounts = await _dbContext.KeyAccessPatterns.AsNoTracking()
            .Where(entity => entity.IsActive)
            .GroupBy(entity => entity.KeyTypeCode)
            .Select(group => new { TypeCode = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TypeCode, item => item.Count, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        return types
            .Select(entity => new KeyTypeListItem(
                entity.TypeCode,
                entity.IsActive,
                activeCounts.TryGetValue(entity.TypeCode, out int count) ? count : 0))
            .ToArray();
    }

    public async Task AddKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keyAsset);
        _dbContext.KeyAssets.Add(DomainCatalogMapper.ToEntity(keyAsset));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keyAsset);
        KeyAssetEntity? entity = await _dbContext.KeyAssets
            .FirstOrDefaultAsync(item => item.KeyAssetId == keyAsset.KeyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The physical key copy was not found in persistence.");
        }

        entity.IsActive = keyAsset.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        KeyAssetEntity? entity = await _dbContext.KeyAssets
            .FirstOrDefaultAsync(item => item.KeyAssetId == keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The physical key copy was not found in persistence.");
        }

        _dbContext.KeyAssets.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountKeyAssetsForKeyNumberAsync(string keyNumber, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAssets.CountAsync(
            entity => entity.KeyNumber == keyNumber,
            cancellationToken);
    }

    public async Task DeleteKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken)
    {
        KeyAccessPatternEntity? entity = await _dbContext.KeyAccessPatterns
            .FirstOrDefaultAsync(item => item.KeyNumber == keyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The KEY # was not found in persistence.");
        }

        _dbContext.KeyAccessPatterns.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeyAsset?> FindKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        KeyAssetEntity? entity = await _dbContext.KeyAssets
            .AsNoTracking()
            .Include(item => item.AccessPattern)
            .ThenInclude(pattern => pattern.KeyType)
            .FirstOrDefaultAsync(item => item.KeyAssetId == keyAssetId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        IReadOnlyList<KeyOpenedRoomItem> rooms = await _roomAssignments
            .ListForKeyNumberAsync(entity.KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        return DomainCatalogMapper.ToDomain(entity, rooms.Select(room => room.RoomCode));
    }

    public async Task<KeyAsset?> FindKeyAssetAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken)
    {
        KeyAssetEntity? entity = await _dbContext.KeyAssets
            .AsNoTracking()
            .Include(item => item.AccessPattern)
            .ThenInclude(pattern => pattern.KeyType)
            .FirstOrDefaultAsync(
                item => item.KeyNumber == keyNumber && item.MedecoKeyCode == medecoKeyCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        IReadOnlyList<KeyOpenedRoomItem> rooms = await _roomAssignments
            .ListForKeyNumberAsync(entity.KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        return DomainCatalogMapper.ToDomain(entity, rooms.Select(room => room.RoomCode));
    }

    public async Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsAsync(CancellationToken cancellationToken)
    {
        List<KeyAssetEntity> keys = await _dbContext.KeyAssets
            .AsNoTracking()
            .Include(entity => entity.AccessPattern)
            .OrderBy(entity => entity.KeyNumber)
            .ThenBy(entity => entity.MedecoKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey = await _roomAssignments
            .ListForKeyNumbersAsync(keys.Select(key => key.KeyNumber).Distinct(StringComparer.Ordinal), cancellationToken)
            .ConfigureAwait(false);

        return keys
            .Select(entity => new KeyAssetListItem(
                entity.KeyAssetId,
                entity.KeyNumber,
                entity.MedecoKeyCode,
                entity.AccessPattern.KeyTypeCode,
                entity.IsActive,
                roomsByKey.TryGetValue(entity.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : []))
            .ToArray();
    }

    public async Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsForPatternAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        List<KeyAssetEntity> keys = await _dbContext.KeyAssets
            .AsNoTracking()
            .Include(entity => entity.AccessPattern)
            .Where(entity => entity.KeyNumber == keyNumber)
            .OrderBy(entity => entity.MedecoKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<KeyOpenedRoomItem> rooms = await _roomAssignments
            .ListForKeyNumberAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);

        return keys
            .Select(entity => new KeyAssetListItem(
                entity.KeyAssetId,
                entity.KeyNumber,
                entity.MedecoKeyCode,
                entity.AccessPattern.KeyTypeCode,
                entity.IsActive,
                rooms))
            .ToArray();
    }
}
