namespace KeyInventory.Application.Workforce;

/// <summary>
/// Internal opaque identifiers for Party and WorkforceMember. Not operator-entered business data.
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
}
