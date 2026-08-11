using KeyInventory.Application.Workforce;

namespace KeyInventory.Web.Presentation;

public static class RoomDisplayFormatter
{
    public static string Format(RoomListItem room)
    {
        ArgumentNullException.ThrowIfNull(room);
        return Format(room.RoomNumber, room.Description);
    }

    public static string Format(string roomNumber, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return roomNumber;
        }

        return $"{roomNumber} — {description}";
    }
}
