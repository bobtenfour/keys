using System.Text.RegularExpressions;

namespace KeyInventory.Domain.Parties;

/// <summary>
/// Party boundary — persistent person identity for workforce key recipients.
/// Owns FirstName, LastName, and UIN. Does not own workforce relationship authority.
/// </summary>
public sealed class Party
{
    private static readonly Regex NineDigitUin = new("^[0-9]{9}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public Party(string partyCode, string firstName, string lastName, string uin)
    {
        PartyCode = PartyText.Require(partyCode, nameof(partyCode));
        FirstName = PartyText.Require(firstName, nameof(firstName));
        LastName = PartyText.Require(lastName, nameof(lastName));
        Uin = RequireUin(uin);
        IsActive = true;
    }

    public string PartyCode { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Uin { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string firstName, string lastName)
    {
        FirstName = PartyText.Require(firstName, nameof(firstName));
        LastName = PartyText.Require(lastName, nameof(lastName));
    }

    /// <summary>
    /// Corrects UIN on the same Party. Does not replace Party identity or relationships.
    /// </summary>
    public void CorrectUin(string newUin)
    {
        Uin = RequireUin(newUin);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire()
    {
        IsActive = false;
    }

    private static string RequireUin(string? uin)
    {
        string normalized = PartyText.Require(uin, nameof(uin));
        if (!NineDigitUin.IsMatch(normalized))
        {
            throw new ArgumentException("UIN must be exactly nine numeric digits.", nameof(uin));
        }

        return normalized;
    }
}
