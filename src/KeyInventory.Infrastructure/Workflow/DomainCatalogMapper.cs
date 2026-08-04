using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;

namespace KeyInventory.Infrastructure.Workflow;

internal static class DomainCatalogMapper
{
    internal static KeyType ToDomain(KeyTypeEntity entity)
    {
        KeyType keyType = new(entity.TypeCode);
        if (!entity.IsActive)
        {
            keyType.Retire(hasActiveKeyAssets: false);
        }

        return keyType;
    }

    internal static KeyAsset ToDomain(KeyAssetEntity entity)
    {
        KeyType keyType = ToDomain(entity.KeyType);
        KeyAsset keyAsset = new(entity.CatalogKeyCode, keyType);
        if (!entity.IsActive)
        {
            keyAsset.Retire();
        }

        return keyAsset;
    }

    internal static KeyTypeEntity ToEntity(KeyType keyType)
    {
        return new KeyTypeEntity
        {
            TypeCode = keyType.TypeCode,
            IsActive = keyType.IsActive
        };
    }

    internal static KeyAssetEntity ToEntity(KeyAsset keyAsset)
    {
        return new KeyAssetEntity
        {
            CatalogKeyCode = keyAsset.CatalogKeyCode,
            KeyTypeCode = keyAsset.KeyType.TypeCode,
            IsActive = keyAsset.IsActive
        };
    }
}
