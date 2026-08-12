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
            keyType.Retire(hasActiveKeyAccessPatterns: false);
        }

        return keyType;
    }

    internal static KeyAccessPattern ToDomain(
        KeyAccessPatternEntity entity,
        IEnumerable<string> openedRoomCodes)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(openedRoomCodes);

        // Domain construction requires an active KeyType; historical rows may reference a later-retired type.
        KeyType keyType = new(entity.KeyType.TypeCode);
        KeyAccessPattern pattern = new(entity.KeyNumber, keyType);
        foreach (string roomCode in openedRoomCodes)
        {
            pattern.AssignOpenedRoom(roomCode);
        }

        if (!entity.IsActive)
        {
            pattern.Retire(hasActivePhysicalCopies: false);
        }

        return pattern;
    }

    internal static KeyAsset ToDomain(
        KeyAssetEntity entity,
        IEnumerable<string> openedRoomCodes)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(openedRoomCodes);

        // Build pattern as active first so physical-copy construction is allowed, then apply inactive flags.
        KeyType keyType = new(entity.AccessPattern.KeyType.TypeCode);
        KeyAccessPattern pattern = new(entity.AccessPattern.KeyNumber, keyType);
        foreach (string roomCode in openedRoomCodes)
        {
            pattern.AssignOpenedRoom(roomCode);
        }

        KeyAsset keyAsset = new(entity.KeyAssetId, pattern, entity.MedecoKeyCode);
        if (!entity.IsActive)
        {
            keyAsset.Retire();
        }

        if (!entity.AccessPattern.IsActive)
        {
            pattern.Retire(hasActivePhysicalCopies: false);
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

    internal static KeyAccessPatternEntity ToEntity(KeyAccessPattern pattern)
    {
        return new KeyAccessPatternEntity
        {
            KeyNumber = pattern.KeyNumber,
            KeyTypeCode = pattern.KeyType.TypeCode,
            IsActive = pattern.IsActive
        };
    }

    internal static KeyAssetEntity ToEntity(KeyAsset keyAsset)
    {
        return new KeyAssetEntity
        {
            KeyAssetId = keyAsset.KeyAssetId,
            KeyNumber = keyAsset.KeyNumber,
            MedecoKeyCode = keyAsset.MedecoKeyCode,
            IsActive = keyAsset.IsActive
        };
    }
}
