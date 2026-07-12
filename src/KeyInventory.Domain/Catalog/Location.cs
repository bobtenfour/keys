namespace KeyInventory.Domain.Catalog;

public sealed class Location
{
    public Location(string locationCode, Location? parentLocation = null)
    {
        LocationCode = CatalogText.Require(locationCode, nameof(locationCode));
        IsActive = true;
        SetParent(parentLocation);
    }

    public string LocationCode { get; }

    public Location? ParentLocation { get; private set; }

    public bool IsActive { get; private set; }

    public void SetParent(Location? parentLocation)
    {
        if (ReferenceEquals(this, parentLocation))
        {
            throw new InvalidOperationException("Location cannot be its own parent.");
        }

        for (Location? ancestor = parentLocation; ancestor is not null; ancestor = ancestor.ParentLocation)
        {
            if (ReferenceEquals(this, ancestor))
            {
                throw new InvalidOperationException("Location hierarchy cannot contain cycles.");
            }
        }

        ParentLocation = parentLocation;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire(bool hasActiveChildLocations)
    {
        if (hasActiveChildLocations)
        {
            throw new InvalidOperationException(
                "Location cannot be retired while active child Location records require it for hierarchy assignment.");
        }

        IsActive = false;
    }
}
