namespace KeyInventory.Application.Lookup;

public sealed record PartyHolderDisplay(
    string FirstName,
    string LastName,
    string Uin);

public sealed record KeyLookupResult(
    string CatalogKeyCode,
    string TypeCode,
    string AvailabilityStatus,
    PartyHolderDisplay? CurrentHolder,
    string? OpenLoanCode);

public sealed record OperationalLoanDisplay(
    string LoanCode,
    string CatalogKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    string Status,
    DateTimeOffset? ReturnedAtUtc);

public sealed record IssuedKeyForMemberItem(
    string LoanCode,
    string CatalogKeyCode,
    string HolderFirstName,
    string HolderLastName,
    string HolderUin,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc);

public sealed record WorkforceMemberIdentityDisplay(
    string WorkforceMemberCode,
    string FirstName,
    string LastName,
    string Uin,
    string Status);

public static class OperationalKeyAvailability
{
    public const string Available = "Available";
    public const string Issued = "Issued";
}

public static class PartyHolderDisplayFormatter
{
    public static string Format(PartyHolderDisplay holder)
    {
        ArgumentNullException.ThrowIfNull(holder);
        return $"{holder.FirstName} {holder.LastName} ({holder.Uin})";
    }

    public static string Format(string firstName, string lastName, string uin)
    {
        return Format(new PartyHolderDisplay(firstName, lastName, uin));
    }
}
