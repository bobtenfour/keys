using KeyInventory.Application.Catalog;

namespace KeyInventory.Application.Lookup;

/// <summary>
/// Typed global operator search presentation DTO composed from existing authorities.
/// </summary>
public sealed record GlobalOperatorSearchResult(
    string Query,
    IReadOnlyList<GlobalPersonSearchHit> People,
    IReadOnlyList<GlobalRoomSearchHit> Rooms,
    IReadOnlyList<GlobalKeyNumberSearchHit> KeyNumbers,
    IReadOnlyList<GlobalMedecoSearchHit> MedecoCopies)
{
    public bool HasAnyResults =>
        People.Count > 0
        || Rooms.Count > 0
        || KeyNumbers.Count > 0
        || MedecoCopies.Count > 0;
}

public sealed record GlobalPersonSearchHit(
    string WorkforceMemberCode,
    string FirstName,
    string LastName,
    string Uin,
    string DepartmentCode,
    string Status,
    IReadOnlyList<GlobalPersonCurrentKey> CurrentKeys);

public sealed record GlobalPersonCurrentKey(
    string KeyNumber,
    string MedecoKeyCode,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public sealed record GlobalRoomSearchHit(
    string RoomCode,
    string RoomNumber,
    string? Description,
    IReadOnlyList<string> OpeningKeyNumbers);

public sealed record GlobalKeyNumberSearchHit(
    string KeyNumber,
    string TypeCode,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms,
    IReadOnlyList<GlobalPhysicalCopyState> Copies);

public sealed record GlobalPhysicalCopyState(
    string MedecoKeyCode,
    string AvailabilityStatus,
    PartyHolderDisplay? CurrentHolder);

public sealed record GlobalMedecoSearchHit(
    string KeyNumber,
    string MedecoKeyCode,
    string TypeCode,
    string AvailabilityStatus,
    PartyHolderDisplay? CurrentHolder,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);
