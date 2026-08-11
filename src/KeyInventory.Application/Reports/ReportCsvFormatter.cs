using System.Globalization;
using System.Text;

namespace KeyInventory.Application.Reports;

/// <summary>
/// Shared CSV formatting authority for REPORTS-1. Escapes values and builds tabular CSV text.
/// </summary>
public static class ReportCsvFormatter
{
    public static string Escape(string? value)
    {
        string text = value ?? string.Empty;
        bool mustQuote = text.Contains('"', StringComparison.Ordinal)
            || text.Contains(',', StringComparison.Ordinal)
            || text.Contains('\r', StringComparison.Ordinal)
            || text.Contains('\n', StringComparison.Ordinal);
        if (!mustQuote)
        {
            return text;
        }

        return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    public static string FormatTimestamp(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
    }

    public static string Build(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        StringBuilder builder = new();
        builder.Append(string.Join(',', headers.Select(Escape)));
        builder.Append('\n');
        foreach (IReadOnlyList<string> row in rows)
        {
            builder.Append(string.Join(',', row.Select(Escape)));
            builder.Append('\n');
        }

        return builder.ToString();
    }
}
