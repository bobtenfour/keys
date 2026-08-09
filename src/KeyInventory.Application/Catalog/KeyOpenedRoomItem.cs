namespace KeyInventory.Application.Catalog;

/// <summary>
/// Current Room opened by a KeyAsset. Building is derived through Room only.
/// </summary>
public sealed record KeyOpenedRoomItem(
    string RoomCode,
    string BuildingCode,
    string RoomNumber);

public static class KeyOpenedRoomDisplayFormatter
{
    public static string Format(IReadOnlyList<KeyOpenedRoomItem> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        if (rooms.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "; ",
            rooms
                .OrderBy(room => room.BuildingCode, StringComparer.Ordinal)
                .ThenBy(room => room.RoomNumber, StringComparer.Ordinal)
                .Select(room => $"{room.BuildingCode}/{room.RoomNumber}"));
    }
}
