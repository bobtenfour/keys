using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure.Data;

namespace KeyInventory.Infrastructure.Workflow;

internal static class DomainCatalogMapper
{
    internal static KeyAccessPattern ToDomain(KeyAccessPatternEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        KeyAccessClassification classification = ParseClassification(entity.Classification);
        KeyAccessPattern pattern = new(entity.KeyNumber, classification, entity.RoomCode);

        if (!entity.IsActive)
        {
            pattern.Retire(hasActivePhysicalCopies: false);
        }

        return pattern;
    }

    internal static KeyAsset ToDomain(KeyAssetEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(entity.AccessPattern);

        KeyAccessPattern pattern = ToDomain(entity.AccessPattern);
        KeyPhysicalCondition condition = ParseCondition(entity.Condition);
        return KeyAsset.Rehydrate(
            entity.KeyAssetId,
            pattern,
            entity.MedecoKeyCode,
            condition,
            entity.ReplacesKeyAssetId);
    }

    internal static KeyAccessPatternEntity ToEntity(KeyAccessPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        return new KeyAccessPatternEntity
        {
            KeyNumber = pattern.KeyNumber,
            Classification = pattern.Classification.ToString(),
            RoomCode = pattern.RoomCode,
            IsActive = pattern.IsActive
        };
    }

    internal static KeyAssetEntity ToEntity(KeyAsset keyAsset)
    {
        ArgumentNullException.ThrowIfNull(keyAsset);
        return new KeyAssetEntity
        {
            KeyAssetId = keyAsset.KeyAssetId,
            KeyNumber = keyAsset.KeyNumber,
            MedecoKeyCode = keyAsset.MedecoKeyCode,
            Condition = keyAsset.Condition.ToString(),
            ReplacesKeyAssetId = keyAsset.ReplacesKeyAssetId
        };
    }

    internal static KeyAccessClassification ParseClassification(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("KeyAccessPattern.Classification value is missing.");
        }

        if (!Enum.TryParse(value.Trim(), ignoreCase: false, out KeyAccessClassification classification)
            || classification is not (KeyAccessClassification.Regular or KeyAccessClassification.Master))
        {
            throw new InvalidOperationException(
                $"Unsupported KEY # classification '{value}'. Must be 'Regular' or 'Master'.");
        }

        return classification;
    }

    internal static KeyPhysicalCondition ParseCondition(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("KeyAsset.Condition value is missing.");
        }

        if (!Enum.TryParse(value.Trim(), ignoreCase: false, out KeyPhysicalCondition condition)
            || condition is not (
                KeyPhysicalCondition.Active
                or KeyPhysicalCondition.Lost
                or KeyPhysicalCondition.Destroyed))
        {
            throw new InvalidOperationException(
                $"Unsupported key condition '{value}'. Must be 'Active', 'Lost', or 'Destroyed'.");
        }

        return condition;
    }
}
