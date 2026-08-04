using System.Globalization;

namespace KeyInventory.Web.Presentation;

public static class OperatorTimestampFormatter
{
    public static string ToFriendlyDisplay(DateTimeOffset valueUtc, DateTimeOffset? nowUtc = null)
    {
        DateTimeOffset now = nowUtc ?? DateTimeOffset.UtcNow;
        DateTimeOffset localValue = valueUtc.ToLocalTime();
        DateTimeOffset localNow = now.ToLocalTime();
        TimeSpan age = localNow - localValue;

        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        if (age < TimeSpan.FromMinutes(1))
        {
            return "Just now";
        }

        if (age < TimeSpan.FromHours(1))
        {
            int minutes = Math.Max(1, (int)Math.Floor(age.TotalMinutes));
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }

        if (localValue.Date == localNow.Date)
        {
            return $"Today {localValue.ToString("h:mm tt", CultureInfo.InvariantCulture)}";
        }

        if (localValue.Date == localNow.Date.AddDays(-1))
        {
            return $"Yesterday {localValue.ToString("h:mm tt", CultureInfo.InvariantCulture)}";
        }

        if (localValue.Year == localNow.Year)
        {
            return localValue.ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);
        }

        return localValue.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }
}