using ClosedXML.Excel;
using KeyInventory.Application.Reports;

namespace KeyInventory.Infrastructure.Reports;

public sealed class ClosedXmlReportExcelExporter : IReportExcelExporter
{
    public byte[] Export(ReportExportTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        using XLWorkbook workbook = new();
        IXLWorksheet worksheet = workbook.Worksheets.Add(SanitizeSheetName(table.WorksheetName));

        int rowIndex = 1;
        worksheet.Cell(rowIndex, 1).Value = table.Title;
        worksheet.Cell(rowIndex, 1).Style.Font.Bold = true;
        worksheet.Cell(rowIndex, 1).Style.Font.FontSize = 14;
        rowIndex += 2;

        if (!string.IsNullOrWhiteSpace(table.FilterContext))
        {
            worksheet.Cell(rowIndex, 1).Value = "Filters";
            worksheet.Cell(rowIndex, 1).Style.Font.Bold = true;
            rowIndex++;
            worksheet.Cell(rowIndex, 1).Value = table.FilterContext;
            rowIndex += 2;
        }

        int headerRow = rowIndex;
        for (int column = 0; column < table.Headers.Count; column++)
        {
            IXLCell headerCell = worksheet.Cell(headerRow, column + 1);
            headerCell.Value = table.Headers[column];
            headerCell.Style.Font.Bold = true;
        }

        rowIndex = headerRow + 1;
        foreach (IReadOnlyList<ReportExportCell> row in table.Rows)
        {
            for (int column = 0; column < table.Headers.Count; column++)
            {
                ReportExportCell cell = column < row.Count
                    ? row[column]
                    : ReportExportCell.FromText(string.Empty);
                IXLCell excelCell = worksheet.Cell(rowIndex, column + 1);
                WriteCell(excelCell, cell);
            }

            rowIndex++;
        }

        if (table.Headers.Count > 0)
        {
            worksheet.Columns(1, table.Headers.Count).AdjustToContents();
        }

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteCell(IXLCell excelCell, ReportExportCell cell)
    {
        switch (cell.Kind)
        {
            case ReportExportCellKind.WholeNumber when cell.IntegerValue is int integerValue:
                excelCell.Value = integerValue;
                break;
            case ReportExportCellKind.DateTimeUtc when cell.DateTimeUtc is DateTimeOffset dateTime:
                excelCell.Value = dateTime.UtcDateTime;
                excelCell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm \"UTC\"";
                break;
            default:
                excelCell.Value = cell.Text;
                break;
        }
    }

    private static string SanitizeSheetName(string worksheetName)
    {
        string name = string.IsNullOrWhiteSpace(worksheetName) ? "Report" : worksheetName.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars().Concat([':', '\\', '/', '?', '*', '[', ']']))
        {
            name = name.Replace(invalid, ' ');
        }

        name = name.Trim();
        if (name.Length == 0)
        {
            name = "Report";
        }

        return name.Length <= 31 ? name : name[..31];
    }
}
