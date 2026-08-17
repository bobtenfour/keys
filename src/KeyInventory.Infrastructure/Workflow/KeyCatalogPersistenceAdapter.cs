using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Workflow;

public sealed class KeyCatalogPersistenceAdapter : IKeyCatalogPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyAccessResolutionPort _accessResolution;

    public KeyCatalogPersistenceAdapter(
        KeyInventoryDbContext dbContext,
        IKeyAccessResolutionPort accessResolution)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _accessResolution = accessResolution ?? throw new ArgumentNullException(nameof(accessResolution));
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
            .FirstOrDefaultAsync(item => item.KeyNumber == keyNumber, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : DomainCatalogMapper.ToDomain(entity);
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

        entity.Classification = pattern.Classification.ToString();
        entity.RoomCode = pattern.RoomCode;
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

        return await ToPatternListItemsAsync(patterns, expandMaster: false, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<KeyAccessPatternListItem>> SearchActiveKeyAccessPatternsAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (maxResults < 1)
        {
            return [];
        }

        string term = (searchText ?? string.Empty).Trim();
        int bound = Math.Min(maxResults, 25);

        IQueryable<KeyAccessPatternEntity> query = _dbContext.KeyAccessPatterns.AsNoTracking()
            .Where(entity => entity.IsActive);

        if (term.Length > 0)
        {
            query = query.Where(entity => entity.KeyNumber.Contains(term));
        }

        List<KeyAccessPatternEntity> patterns = await query
            .OrderBy(entity => entity.KeyNumber)
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await ToPatternListItemsAsync(patterns, expandMaster: false, cancellationToken)
            .ConfigureAwait(false);
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

    public async Task AddKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keyAsset);
        _dbContext.KeyAssets.Add(DomainCatalogMapper.ToEntity(keyAsset));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddNewKeyNumberWithFirstKeyAsync(
        KeyAccessPattern pattern,
        KeyAsset firstKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(firstKey);

        if (!string.Equals(pattern.KeyNumber, firstKey.KeyNumber, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The first key must belong to the new KEY #.");
        }

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _dbContext.KeyAccessPatterns.Add(DomainCatalogMapper.ToEntity(pattern));
            _dbContext.KeyAssets.Add(DomainCatalogMapper.ToEntity(firstKey));
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
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

        entity.Condition = keyAsset.Condition.ToString();
        entity.ReplacesKeyAssetId = keyAsset.ReplacesKeyAssetId;
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
            .FirstOrDefaultAsync(item => item.KeyAssetId == keyAssetId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : DomainCatalogMapper.ToDomain(entity);
    }

    public async Task<KeyAsset?> FindKeyAssetAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken)
    {
        KeyAssetEntity? entity = await _dbContext.KeyAssets
            .AsNoTracking()
            .Include(item => item.AccessPattern)
            .FirstOrDefaultAsync(
                item => item.KeyNumber == keyNumber && item.MedecoKeyCode == medecoKeyCode,
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : DomainCatalogMapper.ToDomain(entity);
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

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await ResolveRoomsForAssetsAsync(keys, cancellationToken).ConfigureAwait(false);

        return keys
            .Select(entity =>
            {
                KeyAccessClassification classification =
                    DomainCatalogMapper.ParseClassification(entity.AccessPattern.Classification);
                return new KeyAssetListItem(
                    entity.KeyAssetId,
                    entity.KeyNumber,
                    entity.MedecoKeyCode,
                    classification,
                    DomainCatalogMapper.ParseCondition(entity.Condition),
                    entity.ReplacesKeyAssetId,
                    roomsByKey.TryGetValue(entity.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                        ? rooms
                        : []);
            })
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

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await ResolveRoomsForAssetsAsync(keys, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<KeyOpenedRoomItem> rooms =
            roomsByKey.TryGetValue(keyNumber, out IReadOnlyList<KeyOpenedRoomItem>? opened)
                ? opened
                : [];

        return keys
            .Select(entity => new KeyAssetListItem(
                entity.KeyAssetId,
                entity.KeyNumber,
                entity.MedecoKeyCode,
                DomainCatalogMapper.ParseClassification(entity.AccessPattern.Classification),
                DomainCatalogMapper.ParseCondition(entity.Condition),
                entity.ReplacesKeyAssetId,
                rooms))
            .ToArray();
    }

    private async Task<IReadOnlyList<KeyAccessPatternListItem>> ToPatternListItemsAsync(
        List<KeyAccessPatternEntity> patterns,
        bool expandMaster,
        CancellationToken cancellationToken)
    {
        if (patterns.Count == 0)
        {
            return [];
        }

        Dictionary<string, int> copyCounts = await _dbContext.KeyAssets.AsNoTracking()
            .Where(entity => patterns.Select(p => p.KeyNumber).Contains(entity.KeyNumber))
            .GroupBy(entity => entity.KeyNumber)
            .Select(group => new { KeyNumber = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.KeyNumber, item => item.Count, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await _accessResolution.ResolveForPatternsAsync(
                    patterns.Select(entity => new KeyAccessResolutionRequest(
                        entity.KeyNumber,
                        DomainCatalogMapper.ParseClassification(entity.Classification),
                        entity.RoomCode)),
                    expandMaster,
                    cancellationToken)
                .ConfigureAwait(false);

        return patterns
            .Select(entity => new KeyAccessPatternListItem(
                entity.KeyNumber,
                DomainCatalogMapper.ParseClassification(entity.Classification),
                entity.IsActive,
                copyCounts.TryGetValue(entity.KeyNumber, out int count) ? count : 0,
                roomsByKey.TryGetValue(entity.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : []))
            .ToArray();
    }

    private Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ResolveRoomsForAssetsAsync(
        List<KeyAssetEntity> keys,
        CancellationToken cancellationToken)
    {
        IEnumerable<KeyAccessResolutionRequest> requests = keys
            .GroupBy(key => key.KeyNumber, StringComparer.Ordinal)
            .Select(group =>
            {
                KeyAssetEntity first = group.First();
                return new KeyAccessResolutionRequest(
                    first.KeyNumber,
                    DomainCatalogMapper.ParseClassification(first.AccessPattern.Classification),
                    first.AccessPattern.RoomCode);
            });

        return _accessResolution.ResolveForPatternsAsync(requests, expandMaster: false, cancellationToken);
    }
}
