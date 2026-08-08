namespace KeyInventory.Infrastructure.Data;

public sealed class PartyEntity
{
    public string PartyCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Uin { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
