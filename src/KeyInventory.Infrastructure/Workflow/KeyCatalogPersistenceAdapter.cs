using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Workflow;

public sealed class KeyCatalogPersistenceAdapter : IKeyCatalogPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;

    public KeyCatalogPersistenceAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

        return entity is null ? null : DomainCatalogMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsAsync(CancellationToken cancellationToken)
    {
        List<KeyAssetListItem> items = await _dbContext.KeyAssets
            .AsNoTracking()
            .OrderBy(entity => entity.CatalogKeyCode)
            .Select(entity => new KeyAssetListItem(entity.CatalogKeyCode, entity.KeyTypeCode, entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }
}
