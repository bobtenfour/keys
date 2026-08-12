using KeyInventory.Domain.Catalog;
using Xunit;
using CatalogLock = KeyInventory.Domain.Catalog.Lock;

namespace KeyInventory.ArchitectureTests;

public sealed class CatalogDomainInvariantTests
{
    [Fact]
    public void KeyAssetRequiresMedecoKeyCode()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800");

        Assert.Throws<ArgumentException>(() => new KeyAsset(Guid.NewGuid(), pattern, " "));
    }

    [Fact]
    public void KeyAssetRequiresNonEmptyKeyAssetId()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800");

        Assert.Throws<ArgumentException>(() => new KeyAsset(Guid.Empty, pattern, "26"));
    }

    [Fact]
    public void KeyAssetRequiresActiveKeyAccessPattern()
    {
        KeyType keyType = new("mechanical");
        KeyAccessPattern pattern = new("66800", keyType);
        pattern.Retire(hasActivePhysicalCopies: false);

        Assert.Throws<InvalidOperationException>(() => new KeyAsset(Guid.NewGuid(), pattern, "26"));
    }

    [Fact]
    public void KeyAccessPatternRequiresActiveKeyType()
    {
        KeyType keyType = new("mechanical");
        keyType.Retire(hasActiveKeyAccessPatterns: false);

        Assert.Throws<InvalidOperationException>(() => new KeyAccessPattern("66800", keyType));
    }

    [Fact]
    public void KeyTypeCannotRetireWhileActiveKeyAccessPatternsRequireIt()
    {
        KeyType keyType = new("mechanical");

        Assert.Throws<InvalidOperationException>(() => keyType.Retire(hasActiveKeyAccessPatterns: true));
    }

    [Fact]
    public void KeySeriesCannotRetireWhileActiveKeyAssetsReferenceIt()
    {
        KeySeries keySeries = new("master-a");

        Assert.Throws<InvalidOperationException>(() => keySeries.Retire(hasActiveKeyAssets: true));
    }

    [Fact]
    public void LockRequiresActiveLocation()
    {
        Location location = new("hq");
        location.Retire(hasActiveChildLocations: false);

        Assert.Throws<InvalidOperationException>(() => new CatalogLock("front-door", location));
    }

    [Fact]
    public void LocationCannotBeItsOwnParent()
    {
        Location location = new("hq");

        Assert.Throws<InvalidOperationException>(() => location.SetParent(location));
    }

    [Fact]
    public void LocationHierarchyCannotContainCycles()
    {
        Location root = new("hq");
        Location child = new("hq-floor-1", root);

        Assert.Throws<InvalidOperationException>(() => root.SetParent(child));
    }

    [Fact]
    public void LocationCannotRetireWhileActiveChildLocationsRequireIt()
    {
        Location location = new("hq");

        Assert.Throws<InvalidOperationException>(() => location.Retire(hasActiveChildLocations: true));
    }

    [Fact]
    public void KeyAccessPatternOwnsRoomAssignmentsWithZeroOneAndManyCardinality()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800");

        Assert.Empty(pattern.OpenedRoomCodes);

        pattern.AssignOpenedRoom("room-a");
        Assert.Equal(["room-a"], pattern.OpenedRoomCodes.Order(StringComparer.Ordinal));

        pattern.AssignOpenedRoom("room-b");
        Assert.Equal(["room-a", "room-b"], pattern.OpenedRoomCodes.Order(StringComparer.Ordinal));

        pattern.RemoveOpenedRoom("room-a");
        Assert.Equal(["room-b"], pattern.OpenedRoomCodes.Order(StringComparer.Ordinal));

        pattern.RemoveOpenedRoom("room-b");
        Assert.Empty(pattern.OpenedRoomCodes);
    }

    [Fact]
    public void KeyAccessPatternRejectsDuplicateRoomAssignment()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800");
        pattern.AssignOpenedRoom("room-a");

        Assert.Throws<InvalidOperationException>(() => pattern.AssignOpenedRoom("room-a"));
    }

    [Fact]
    public void MultipleKeyAccessPatternsMayOpenTheSameRoom()
    {
        KeyAccessPattern first = CatalogTestFactory.CreatePattern("66800");
        KeyAccessPattern second = CatalogTestFactory.CreatePattern("66801");

        first.AssignOpenedRoom("shared-room");
        second.AssignOpenedRoom("shared-room");

        Assert.Contains("shared-room", first.OpenedRoomCodes);
        Assert.Contains("shared-room", second.OpenedRoomCodes);
    }

    [Fact]
    public void PhysicalCopiesDeriveIdenticalRoomsFromParentKeyAccessPattern()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800");
        pattern.AssignOpenedRoom("410D");
        pattern.AssignOpenedRoom("411A");

        KeyAsset copyA = new(Guid.NewGuid(), pattern, "26");
        KeyAsset copyB = new(Guid.NewGuid(), pattern, "27");

        Assert.Equal(pattern.OpenedRoomCodes, copyA.OpenedRoomCodes);
        Assert.Equal(pattern.OpenedRoomCodes, copyB.OpenedRoomCodes);
        Assert.Equal(copyA.OpenedRoomCodes.Order(StringComparer.Ordinal), copyB.OpenedRoomCodes.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void KeyAssetDoesNotOwnRoomAssignmentMutatorsOrBuilding()
    {
        Assert.DoesNotContain(
            typeof(KeyAsset).GetMethods(),
            method => method.Name is "AssignOpenedRoom" or "RemoveOpenedRoom");
        Assert.DoesNotContain(
            typeof(KeyAsset).GetProperties(),
            property => string.Equals(property.Name, "Building", StringComparison.Ordinal)
                || string.Equals(property.Name, "BuildingCode", StringComparison.Ordinal)
                || string.Equals(property.Name, "IntendedLock", StringComparison.Ordinal)
                || string.Equals(property.Name, "CatalogKeyCode", StringComparison.Ordinal));

        Assert.DoesNotContain(
            typeof(KeyType).GetMethods(),
            method => method.Name.Contains("Room", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(KeyType).GetProperties(),
            property => property.Name.Contains("Room", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SameMedecoIsAllowedUnderDifferentKeyNumbersAtDomainLevel()
    {
        KeyAsset first = CatalogTestFactory.CreateCopy("66800", "26", "mechanical");
        KeyAsset second = CatalogTestFactory.CreateCopy("66801", "26", "mechanical");

        Assert.Equal("26", first.MedecoKeyCode);
        Assert.Equal("26", second.MedecoKeyCode);
        Assert.NotEqual(first.KeyNumber, second.KeyNumber);
        Assert.NotEqual(first.KeyAssetId, second.KeyAssetId);
    }

    [Fact]
    public void DomainAllowsConstructingDistinctCopiesUnderSameKeyNumberWithDifferentMedeco()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800", "mechanical");
        KeyAsset first = new(Guid.NewGuid(), pattern, "26");
        KeyAsset second = new(Guid.NewGuid(), pattern, "27");

        Assert.Equal("66800", first.KeyNumber);
        Assert.Equal("66800", second.KeyNumber);
        Assert.NotEqual(first.MedecoKeyCode, second.MedecoKeyCode);
        Assert.Same(pattern, first.AccessPattern);
        Assert.Same(pattern, second.AccessPattern);
    }
}
