using KeyInventory.Application.Catalog;
using KeyInventory.Domain.Catalog;

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
    IReadOnlyList<GlobalPersonWorkAssignment> WorkAssignments,
    IReadOnlyList<GlobalPersonCurrentKey> CurrentKeys);

/// <summary>
/// Current active Room Assignment room relationship for a person search hit.
/// Not key custody — assignment authority only.
/// </summary>
public sealed record GlobalPersonWorkAssignment(
    string RoomNumber);

public sealed record GlobalPersonCurrentKey(
    string KeyNumber,
    string MedecoKeyCode,
    KeyAccessClassification Classification,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public sealed record GlobalRoomSearchHit(
    string RoomCode,
    string RoomNumber,
    string? Description,
    string DepartmentCode,
    IReadOnlyList<string> OpeningKeyNumbers);

public sealed record GlobalKeyNumberSearchHit(
    string KeyNumber,
    KeyAccessClassification Classification,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms,
    IReadOnlyList<GlobalPhysicalCopyState> Copies);

public sealed record GlobalPhysicalCopyState(
    string MedecoKeyCode,
    KeyPhysicalCondition Condition,
    string AvailabilityStatus,
    PartyHolderDisplay? CurrentHolder);

public sealed record GlobalMedecoSearchHit(
    string KeyNumber,
    string MedecoKeyCode,
    KeyAccessClassification Classification,
    KeyPhysicalCondition Condition,
    string AvailabilityStatus,
    PartyHolderDisplay? CurrentHolder,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);
