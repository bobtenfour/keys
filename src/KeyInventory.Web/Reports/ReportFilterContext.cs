namespace KeyInventory.Web.Reports;

public static class ReportFilterContext
{
    public static string Key(string? keyFilter)
    {
        return string.IsNullOrWhiteSpace(keyFilter)
            ? "KEY # / MEDECO filter: (none)"
            : $"KEY # / MEDECO filter: {keyFilter.Trim()}";
    }

    public static string Member(string? memberCode)
    {
        return string.IsNullOrWhiteSpace(memberCode)
            ? "Workforce member: (none)"
            : $"Workforce member: {memberCode.Trim()}";
    }

    public static string Status(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "Workforce status: (all)"
            : $"Workforce status: {status.Trim()}";
    }
}
