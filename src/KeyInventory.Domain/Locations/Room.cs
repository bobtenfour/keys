namespace KeyInventory.Domain.Locations;

/// <summary>
/// Location boundary — physical room within one Building.
/// Owns RoomNumber uniqueness within Building and Description.
/// </summary>
public sealed class Room
{
    public Room(string roomCode, Building building, string roomNumber, string? description = null)
    {
        RoomCode = LocationPlaceText.Require(roomCode, nameof(roomCode));
        Building = building ?? throw new ArgumentNullException(nameof(building));
        BuildingCode = building.BuildingCode;
        RoomNumber = LocationPlaceText.Require(roomNumber, nameof(roomNumber));
        Description = LocationPlaceText.NormalizeOptional(description);
        IsActive = true;
    }

    public string RoomCode { get; }

    public Building Building { get; private set; }

    public string BuildingCode { get; }

    public string RoomNumber { get; private set; }

    public string Description { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateDescription(string? description)
    {
        Description = LocationPlaceText.NormalizeOptional(description);
    }

    public void Activate(Building building)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (!string.Equals(building.BuildingCode, BuildingCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Room activation must use the owning Building.");
        }

        if (!building.IsActive)
        {
            throw new InvalidOperationException("Room cannot be active in an inactive Building.");
        }

        Building = building;
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
