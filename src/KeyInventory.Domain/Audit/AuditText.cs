namespace KeyInventory.Domain.Audit;

internal static class AuditText
{
    internal static string Require(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
