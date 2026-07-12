namespace KeyInventory.Domain.Catalog;

public sealed class Lock
{
    public Lock(string lockCode, Location location)
    {
        LockCode = CatalogText.Require(lockCode, nameof(lockCode));
        Location = RequireActiveLocation(location);
        IsActive = true;
    }

    public string LockCode { get; }

    public Location Location { get; private set; }

    public bool IsActive { get; private set; }

    public void AssignLocation(Location location)
    {
        Location = RequireActiveLocation(location);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }

    private static Location RequireActiveLocation(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!location.IsActive)
        {
            throw new InvalidOperationException("Lock cannot reference an inactive Location for new catalog assignment.");
        }

        return location;
    }
}
