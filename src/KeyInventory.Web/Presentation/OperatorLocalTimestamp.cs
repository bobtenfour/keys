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

        if (!DateTime.TryParse(
                localControlValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out DateTime localParsed))
        {
            error = "Enter a valid local date and time.";
            return false;
        }

        DateTime localUnspecified = DateTime.SpecifyKind(localParsed, DateTimeKind.Unspecified);
        DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localUnspecified, TimeZoneInfo.Local);
        utcValue = new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        return true;
    }
}
