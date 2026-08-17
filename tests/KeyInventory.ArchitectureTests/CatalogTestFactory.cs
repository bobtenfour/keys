using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

internal static class CatalogTestFactory
{
    public static KeyAsset CreateCopy(
        string keyNumber,
        string medeco,
        KeyAccessClassification classification = KeyAccessClassification.Regular,
        string? regularRoomCode = "room-default")
    {
        KeyAccessPattern pattern = CreatePattern(keyNumber, classification, regularRoomCode);
        return new KeyAsset(Guid.NewGuid(), pattern, medeco);
    }

    public static KeyAccessPattern CreatePattern(
        string keyNumber,
        KeyAccessClassification classification = KeyAccessClassification.Regular,
        string? regularRoomCode = "room-default")
    {
        string? room = classification == KeyAccessClassification.Master
            ? null
            : regularRoomCode;
        return new KeyAccessPattern(keyNumber, classification, room);
    }
}
