namespace KeyInventory.Application.Workflow;

public sealed record LoanListItem(
    string LoanCode,
    Guid KeyAssetId,
    string KeyNumber,
    string MedecoKeyCode,
    string BorrowerPartyReference,
    string? HolderFirstName,
    string? HolderLastName,
    string? HolderUin,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    string Status);
