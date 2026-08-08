namespace KeyInventory.Domain.Locations;

/// <summary>
/// Location boundary — physical building place that contains Rooms.
/// </summary>
public sealed class Building
{
    public Building(string buildingCode)
    {
        BuildingCode = LocationPlaceText.Require(buildingCode, nameof(buildingCode));
        IsActive = true;
    }

    public string BuildingCode { get; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
