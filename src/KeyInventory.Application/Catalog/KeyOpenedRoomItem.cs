using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

/// <summary>
/// Room opened by a KEY # / KeyAccessPattern (derived for every physical copy under that KEY #).
/// </summary>
public sealed record KeyOpenedRoomItem(
    string RoomCode,
    string RoomNumber,
    string? Description);

public static class KeyOpenedRoomDisplayFormatter
{
    public const string MasterAccessDisplay = "Access: All Rooms";

    /// <summary>
    /// Formats access for operator display. Master always shows All Rooms;
    /// Regular formats the single Room (empty when none resolved).
    /// </summary>
    public static string FormatAccess(
        KeyAccessClassification classification,
        IReadOnlyList<KeyOpenedRoomItem> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        if (classification == KeyAccessClassification.Master)
        {
            return MasterAccessDisplay;
        }

        return Format(rooms);
    }

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
                .OrderBy(room => room.RoomNumber, StringComparer.Ordinal)
                .ThenBy(room => room.RoomCode, StringComparer.Ordinal)
                .Select(FormatRoom));
    }

    public static string FormatRoom(KeyOpenedRoomItem room)
    {
        ArgumentNullException.ThrowIfNull(room);
        if (string.IsNullOrWhiteSpace(room.Description))
        {
            return room.RoomNumber;
        }

        return $"{room.RoomNumber} ({room.Description})";
    }
}
