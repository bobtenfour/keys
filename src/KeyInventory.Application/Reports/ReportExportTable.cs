namespace KeyInventory.Application.Reports;

public enum ReportExportCellKind
{
    Text = 0,
    WholeNumber = 1,
    DateTimeUtc = 2
}

public sealed class ReportExportCell
{
    private ReportExportCell(string text, ReportExportCellKind kind, DateTimeOffset? dateTimeUtc, int? integerValue)
    {
        Text = text;
        Kind = kind;
        DateTimeUtc = dateTimeUtc;
        IntegerValue = integerValue;
    }

    public string Text { get; }

    public ReportExportCellKind Kind { get; }

    public DateTimeOffset? DateTimeUtc { get; }

    public int? IntegerValue { get; }

    public static ReportExportCell FromText(string? value)
    {
        return new ReportExportCell(value ?? string.Empty, ReportExportCellKind.Text, null, null);
    }

    public static ReportExportCell WholeNumber(int value)
    {
        return new ReportExportCell(
            value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ReportExportCellKind.WholeNumber,
            null,
            value);
    }

    public static ReportExportCell DateTimeUtcValue(DateTimeOffset value)
    {
        return new ReportExportCell(
            ReportCsvFormatter.FormatTimestamp(value),
            ReportExportCellKind.DateTimeUtc,
            value.ToUniversalTime(),
            null);
    }

    public static ReportExportCell OptionalDateTimeUtc(DateTimeOffset? value)
    {
        return value is null ? FromText(string.Empty) : DateTimeUtcValue(value.Value);
    }
}

public sealed record ReportExportTable(
    string Title,
    string WorksheetName,
    string? FilterContext,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<ReportExportCell>> Rows);

public interface IReportExcelExporter
{
    byte[] Export(ReportExportTable table);
}

public interface IReportPdfExporter
{
    byte[] Export(ReportExportTable table);
}

public static class ReportExportFormats
{
    public const string Csv = "csv";
    public const string Xlsx = "xlsx";
    public const string Pdf = "pdf";

    public static bool TryNormalize(string? format, out string normalized)
    {
        string candidate = (format ?? string.Empty).Trim().ToUpperInvariant();
        normalized = candidate switch
        {
            "CSV" => Csv,
            "XLSX" => Xlsx,
            "EXCEL" => Xlsx,
            "PDF" => Pdf,
            _ => candidate
        };

        return normalized is Csv or Xlsx or Pdf;
    }
}
