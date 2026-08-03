namespace KeyInventory.Domain;

/// <summary>
/// Shared Domain validation for authoritative UTC business timestamps.
/// Does not convert or normalize values.
/// </summary>
public static class UtcTimestamp
{
    public static DateTimeOffset Require(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("UTC timestamp is required.", parameterName);
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("UTC timestamp offset must be zero.", parameterName);
        }

        return value;
    }
}
