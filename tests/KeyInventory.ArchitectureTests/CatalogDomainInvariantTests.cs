using KeyInventory.Domain.Catalog;
using Xunit;
using CatalogLock = KeyInventory.Domain.Catalog.Lock;

namespace KeyInventory.ArchitectureTests;

public sealed class CatalogDomainInvariantTests
{
    [Fact]
    public void KeyAssetRequiresCatalogKeyCode()
    {
        KeyType keyType = new("mechanical");

        Assert.Throws<ArgumentException>(() => new KeyAsset(" ", keyType));
    }

    [Fact]
    public void KeyAssetRequiresActiveKeyType()
    {
        KeyType keyType = new("mechanical");
        keyType.Retire(hasActiveKeyAssets: false);

        Assert.Throws<InvalidOperationException>(() => new KeyAsset("key-1", keyType));
    }

    [Fact]
    public void KeyAssetRejectsInactiveKeySeriesForNewAssignment()
    {
        KeyType keyType = new("mechanical");
        KeySeries keySeries = new("master-a");
        keySeries.Retire(hasActiveKeyAssets: false);

        Assert.Throws<InvalidOperationException>(() => new KeyAsset("key-1", keyType, keySeries));
    }

    [Fact]
    public void KeyAssetRejectsInactiveLockForNewAssignment()
    {
        KeyType keyType = new("mechanical");
        Location location = new("hq");
        CatalogLock intendedLock = new("front-door", location);
        intendedLock.Retire();

        Assert.Throws<InvalidOperationException>(() => new KeyAsset("key-1", keyType, intendedLock: intendedLock));
    }

    [Fact]
    public void KeyAssetRejectsLockWithInactiveLocationForNewAssignment()
    {
        KeyType keyType = new("mechanical");
        Location location = new("hq");
        CatalogLock intendedLock = new("front-door", location);
        location.Retire(hasActiveChildLocations: false);

        Assert.Throws<InvalidOperationException>(() => new KeyAsset("key-1", keyType, intendedLock: intendedLock));
    }

    [Fact]
    public void KeyTypeCannotRetireWhileActiveKeyAssetsRequireIt()
    {
        KeyType keyType = new("mechanical");

        Assert.Throws<InvalidOperationException>(() => keyType.Retire(hasActiveKeyAssets: true));
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
    public void KeyAssetAllowsZeroOneAndMultipleRoomAssignments()
    {
        KeyType keyType = new("mechanical");
        KeyAsset keyAsset = new("key-rooms", keyType);

        Assert.Empty(keyAsset.OpenedRoomCodes);

        keyAsset.AssignOpenedRoom("room-a");
        Assert.Equal(["room-a"], keyAsset.OpenedRoomCodes.Order(StringComparer.Ordinal));

        keyAsset.AssignOpenedRoom("room-b");
        Assert.Equal(["room-a", "room-b"], keyAsset.OpenedRoomCodes.Order(StringComparer.Ordinal));

        keyAsset.RemoveOpenedRoom("room-a");
        Assert.Equal(["room-b"], keyAsset.OpenedRoomCodes.Order(StringComparer.Ordinal));

        keyAsset.RemoveOpenedRoom("room-b");
        Assert.Empty(keyAsset.OpenedRoomCodes);
    }

    [Fact]
    public void KeyAssetRejectsDuplicateRoomAssignment()
    {
        KeyType keyType = new("mechanical");
        KeyAsset keyAsset = new("key-dup", keyType);
        keyAsset.AssignOpenedRoom("room-a");

        Assert.Throws<InvalidOperationException>(() => keyAsset.AssignOpenedRoom("room-a"));
    }

    [Fact]
    public void MultipleKeyAssetsMayOpenTheSameRoom()
    {
        KeyType keyType = new("mechanical");
        KeyAsset first = new("key-1", keyType);
        KeyAsset second = new("key-2", keyType);

        first.AssignOpenedRoom("shared-room");
        second.AssignOpenedRoom("shared-room");

        Assert.Contains("shared-room", first.OpenedRoomCodes);
        Assert.Contains("shared-room", second.OpenedRoomCodes);
    }

    [Fact]
    public void KeyAssetDoesNotOwnBuildingAndKeyTypeDoesNotOwnRoomAssignments()
    {
        Assert.DoesNotContain(
            typeof(KeyAsset).GetProperties(),
            property => string.Equals(property.Name, "Building", StringComparison.Ordinal)
                || string.Equals(property.Name, "BuildingCode", StringComparison.Ordinal));

        Assert.DoesNotContain(
            typeof(KeyType).GetMethods(),
            method => method.Name.Contains("Room", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(KeyType).GetProperties(),
            property => property.Name.Contains("Room", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RoomAssignmentDoesNotRequireIntendedLock()
    {
        KeyType keyType = new("mechanical");
        KeyAsset keyAsset = new("key-no-lock", keyType);

        Assert.Null(keyAsset.IntendedLock);
        keyAsset.AssignOpenedRoom("room-open");
        Assert.Contains("room-open", keyAsset.OpenedRoomCodes);
    }
}
