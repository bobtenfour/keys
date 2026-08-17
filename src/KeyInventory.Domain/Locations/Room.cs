namespace KeyInventory.Domain.Locations;

/// <summary>
/// Location boundary — physical room for this single-site installation.
/// Owns global RoomNumber uniqueness and Description. RoomCode is immutable technical identity.
/// Every Room belongs to exactly one Department.
/// </summary>
public sealed class Room
{
    public Room(string roomCode, string roomNumber, Guid departmentId, string? description = null)
    {
        RoomCode = LocationPlaceText.Require(roomCode, nameof(roomCode));
        RoomNumber = LocationPlaceText.Require(roomNumber, nameof(roomNumber));
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        DepartmentId = departmentId;
        Description = LocationPlaceText.NormalizeOptional(description);
        IsActive = true;
    }

    public string RoomCode { get; }

    public string RoomNumber { get; private set; }

    public Guid DepartmentId { get; private set; }

    public string Description { get; private set; }

    public bool IsActive { get; private set; }

    public void AssignDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));
        }

        DepartmentId = departmentId;
    }

    public void UpdateRoomNumber(string roomNumber)
    {
        RoomNumber = LocationPlaceText.Require(roomNumber, nameof(roomNumber));
    }

    public void UpdateDescription(string? description)
    {
        Description = LocationPlaceText.NormalizeOptional(description);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }
}
