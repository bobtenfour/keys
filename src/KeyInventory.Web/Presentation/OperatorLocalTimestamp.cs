using System.Globalization;

namespace KeyInventory.Web.Presentation;

/// <summary>
/// Converts between operator-local datetime-local controls and authoritative zero-offset UTC at the Web boundary.
/// </summary>
public static class OperatorLocalTimestamp
{
    public const string ControlFormat = "yyyy-MM-ddTHH:mm";

    public static string ToControlValue(DateTimeOffset utcValue)
    {
        DateTimeOffset local = utcValue.ToLocalTime();
        return local.ToString(ControlFormat, CultureInfo.InvariantCulture);
    }

    public static bool TryParseToUtc(string? localControlValue, out DateTimeOffset utcValue, out string? error)
    {
        utcValue = default;
        error = null;

        if (string.IsNullOrWhiteSpace(localControlValue))
        {
            error = "Enter a date and time.";
            return false;
        }

        string normalized = localControlValue.Trim().Replace("·", " ", StringComparison.Ordinal);
        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (!DateTime.TryParseExact(
                normalized,
                [
                    ControlFormat,
                    "MMM d, yyyy h:mm tt",
                    "MMM dd, yyyy h:mm tt",
                    "M/d/yyyy h:mm tt",
                    "MM/dd/yyyy h:mm tt"
                ],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out DateTime localParsed)
            && !DateTime.TryParse(
                normalized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out localParsed))
        {
            error = "Enter a valid local date and time.";
            return false;
        }

        DateTime localUnspecified = DateTime.SpecifyKind(localParsed, DateTimeKind.Unspecified);
        DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localUnspecified, TimeZoneInfo.Local);
        utcValue = new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        return true;
    }

    public static string ToOperatorEntryValue(DateTimeOffset utcValue)
        => OperatorTimestampFormatter.ToAbsoluteDisplay(utcValue);
}
