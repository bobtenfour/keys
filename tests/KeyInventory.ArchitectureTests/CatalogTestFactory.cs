using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

internal static class CatalogTestFactory
{
    public static KeyAsset CreateCopy(string keyNumber, string medeco, string typeCode = "TYPE")
    {
        KeyType keyType = new(typeCode);
        KeyAccessPattern pattern = new(keyNumber, keyType);
        return new KeyAsset(Guid.NewGuid(), pattern, medeco);
    }

    public static KeyAccessPattern CreatePattern(string keyNumber, string typeCode = "TYPE")
    {
        return new KeyAccessPattern(keyNumber, new KeyType(typeCode));
    }
}
