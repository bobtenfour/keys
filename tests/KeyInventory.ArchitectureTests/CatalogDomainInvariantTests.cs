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
        KeyAccessPattern pattern = new("66800", KeyAccessClassification.Regular, "room-a");
        pattern.Retire(hasActivePhysicalCopies: false);

        Assert.Throws<InvalidOperationException>(() => new KeyAsset(Guid.NewGuid(), pattern, "26"));
    }

    [Fact]
    public void KeyAccessPatternRequiresRegularOrMasterClassification()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new KeyAccessPattern("66800", (KeyAccessClassification)999, "room-a"));
    }

    [Fact]
    public void KeyAccessPatternRetainsClassificationAfterRetire()
    {
        KeyAccessPattern pattern = new("66800", KeyAccessClassification.Master, null);
        pattern.Retire(hasActivePhysicalCopies: false);

        Assert.Equal(KeyAccessClassification.Master, pattern.Classification);
        Assert.False(pattern.IsActive);
        Assert.True(pattern.OpensAllRooms);
        Assert.Null(pattern.RoomCode);
    }

    [Fact]
    public void RegularKeyAccessPatternRequiresExactlyOneRoom()
    {
        Assert.Throws<ArgumentException>(() =>
            new KeyAccessPattern("66800", KeyAccessClassification.Regular, null));
        Assert.Throws<ArgumentException>(() =>
            new KeyAccessPattern("66800", KeyAccessClassification.Regular, " "));

        KeyAccessPattern pattern = new("66800", KeyAccessClassification.Regular, "410D");
        Assert.Equal("410D", pattern.RoomCode);
        Assert.Equal(["410D"], pattern.OpenedRoomCodes);
        Assert.False(pattern.OpensAllRooms);
    }

    [Fact]
    public void MasterKeyAccessPatternForbidsRoomCode()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new KeyAccessPattern("MASTER1", KeyAccessClassification.Master, "410D"));

        KeyAccessPattern pattern = new("MASTER1", KeyAccessClassification.Master, null);
        Assert.Null(pattern.RoomCode);
        Assert.Empty(pattern.OpenedRoomCodes);
        Assert.True(pattern.OpensAllRooms);
    }

    [Fact]
    public void MultipleRegularKeyAccessPatternsMayShareTheSameRoom()
    {
        KeyAccessPattern first = CatalogTestFactory.CreatePattern("66800", regularRoomCode: "shared-room");
        KeyAccessPattern second = CatalogTestFactory.CreatePattern("66801", regularRoomCode: "shared-room");

        Assert.Equal("shared-room", first.RoomCode);
        Assert.Equal("shared-room", second.RoomCode);
    }

    [Fact]
    public void PhysicalCopiesDeriveIdenticalAccessFromParentKeyAccessPattern()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800", regularRoomCode: "410D");

        KeyAsset copyA = new(Guid.NewGuid(), pattern, "26");
        KeyAsset copyB = new(Guid.NewGuid(), pattern, "27");

        Assert.Equal(pattern.OpenedRoomCodes, copyA.OpenedRoomCodes);
        Assert.Equal(pattern.OpenedRoomCodes, copyB.OpenedRoomCodes);
        Assert.Equal(pattern.RoomCode, copyA.AccessPattern.RoomCode);
        Assert.Equal(pattern.OpensAllRooms, copyA.AccessPattern.OpensAllRooms);
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
            typeof(KeyAccessPattern).GetMethods(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.DeclaredOnly),
            method => method.Name is "AssignOpenedRoom" or "RemoveOpenedRoom" or "AssignClassification");
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
    public void SameMedecoIsAllowedUnderDifferentKeyNumbersAtDomainLevel()
    {
        KeyAsset first = CatalogTestFactory.CreateCopy("66800", "26", KeyAccessClassification.Regular);
        KeyAsset second = CatalogTestFactory.CreateCopy("66801", "26", KeyAccessClassification.Regular);

        Assert.Equal("26", first.MedecoKeyCode);
        Assert.Equal("26", second.MedecoKeyCode);
        Assert.NotEqual(first.KeyNumber, second.KeyNumber);
        Assert.NotEqual(first.KeyAssetId, second.KeyAssetId);
    }

    [Fact]
    public void DomainAllowsConstructingDistinctCopiesUnderSameKeyNumberWithDifferentMedeco()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("66800", KeyAccessClassification.Regular);
        KeyAsset first = new(Guid.NewGuid(), pattern, "26");
        KeyAsset second = new(Guid.NewGuid(), pattern, "27");

        Assert.Equal("66800", first.KeyNumber);
        Assert.Equal("66800", second.KeyNumber);
        Assert.NotEqual(first.MedecoKeyCode, second.MedecoKeyCode);
        Assert.Same(pattern, first.AccessPattern);
        Assert.Same(pattern, second.AccessPattern);
    }
}
