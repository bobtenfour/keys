namespace KeyInventory.Domain.Catalog;

public sealed class KeyType
{
    public KeyType(string typeCode)
    {
        TypeCode = CatalogText.Require(typeCode, nameof(typeCode));
        IsActive = true;
    }

    public string TypeCode { get; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire(bool hasActiveKeyAccessPatterns)
    {
        if (hasActiveKeyAccessPatterns)
        {
            throw new InvalidOperationException(
                "KeyType cannot be retired while active KEY # records require it for new catalog assignment.");
        }

        IsActive = false;
    }
}
