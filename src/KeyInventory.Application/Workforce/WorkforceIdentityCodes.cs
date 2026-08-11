namespace KeyInventory.Application.Workforce;

/// <summary>
/// Internal opaque identifiers for Party, WorkforceMember, and Room. Not operator-entered business data.
/// </summary>
public static class WorkforceIdentityCodes
{
    public static string NewPartyCode()
    {
        return $"PARTY-{Guid.NewGuid():D}";
    }

    public static string NewWorkforceMemberCode()
    {
        return $"WM-{Guid.NewGuid():D}";
    }

    public static string NewRoomCode()
    {
        return $"ROOM-{Guid.NewGuid():D}";
    }
}
