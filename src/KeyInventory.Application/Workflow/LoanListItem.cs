namespace KeyInventory.Application.Workflow;

public sealed record LoanListItem(
    string LoanCode,
    string CatalogKeyCode,
    string BorrowerPartyReference,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset DueAtUtc,
    string Status,
    DateTimeOffset? ReturnedAtUtc);
