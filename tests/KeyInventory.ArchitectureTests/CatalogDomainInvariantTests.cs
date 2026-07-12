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
}
