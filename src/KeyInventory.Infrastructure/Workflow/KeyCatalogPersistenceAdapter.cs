using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Workflow;

public sealed class KeyCatalogPersistenceAdapter : IKeyCatalogPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyRoomAssignmentPersistencePort _roomAssignments;

    public KeyCatalogPersistenceAdapter(
        KeyInventoryDbContext dbContext,
        IKeyRoomAssignmentPersistencePort roomAssignments)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _roomAssignments = roomAssignments ?? throw new ArgumentNullException(nameof(roomAssignments));
    }

    public Task<bool> KeyAssetExistsAsync(string catalogKeyCode, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAssets.AnyAsync(
            entity => entity.CatalogKeyCode == catalogKeyCode,
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

    public Task<int> CountActiveKeyAssetsForTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        return _dbContext.KeyAssets.CountAsync(
            entity => entity.KeyTypeCode == typeCode && entity.IsActive,
            cancellationToken);
    }

    public async Task<IReadOnlyList<KeyTypeListItem>> ListKeyTypesAsync(CancellationToken cancellationToken)
    {
        List<KeyTypeEntity> types = await _dbContext.KeyTypes.AsNoTracking()
            .OrderBy(entity => entity.TypeCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, int> activeCounts = await _dbContext.KeyAssets.AsNoTracking()
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

    public async Task<KeyAsset?> FindKeyAssetAsync(string catalogKeyCode, CancellationToken cancellationToken)
    {
        KeyAssetEntity? entity = await _dbContext.KeyAssets
            .AsNoTracking()
            .Include(item => item.KeyType)
            .FirstOrDefaultAsync(item => item.CatalogKeyCode == catalogKeyCode, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        KeyAsset keyAsset = DomainCatalogMapper.ToDomain(entity);
        IReadOnlyList<KeyOpenedRoomItem> rooms = await _roomAssignments
            .ListForKeyAsync(catalogKeyCode, cancellationToken)
            .ConfigureAwait(false);
        foreach (KeyOpenedRoomItem room in rooms)
        {
            keyAsset.AssignOpenedRoom(room.RoomCode);
        }

        return keyAsset;
    }

    public async Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsAsync(CancellationToken cancellationToken)
    {
        List<KeyAssetEntity> keys = await _dbContext.KeyAssets
            .AsNoTracking()
            .OrderBy(entity => entity.CatalogKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey = await _roomAssignments
            .ListForKeysAsync(keys.Select(key => key.CatalogKeyCode), cancellationToken)
            .ConfigureAwait(false);

        return keys
            .Select(entity => new KeyAssetListItem(
                entity.CatalogKeyCode,
                entity.KeyTypeCode,
                entity.IsActive,
                roomsByKey.TryGetValue(entity.CatalogKeyCode, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : []))
            .ToArray();
    }
}
