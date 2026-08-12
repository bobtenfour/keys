namespace KeyInventory.Infrastructure.Data;

public sealed class LoanEntity
{
    public string LoanCode { get; set; } = string.Empty;

    public Guid KeyAssetId { get; set; }

    public string BorrowerPartyReference { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset DueAtUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public KeyAssetEntity KeyAsset { get; set; } = null!;
}
