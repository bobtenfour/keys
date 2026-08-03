namespace KeyInventory.Infrastructure.Data;

public sealed class ReturnEntity
{
    public string ReturnCode { get; set; } = string.Empty;

    public string LoanCode { get; set; } = string.Empty;

    public DateTimeOffset ReturnedAtUtc { get; set; }

    public LoanEntity Loan { get; set; } = null!;
}
