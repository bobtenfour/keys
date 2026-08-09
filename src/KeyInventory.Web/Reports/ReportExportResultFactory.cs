using System.Text;
using KeyInventory.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace KeyInventory.Web.Reports;

public static class ReportExportResultFactory
{
    public static FileContentResult CreateCsv(string fileNameWithoutExtension, string csvContent)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent ?? string.Empty);
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = EnsureExtension(fileNameWithoutExtension, ".csv")
        };
    }

    public static FileContentResult CreateXlsx(string fileNameWithoutExtension, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new FileContentResult(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = EnsureExtension(fileNameWithoutExtension, ".xlsx")
        };
    }

    public static FileContentResult CreatePdf(string fileNameWithoutExtension, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new FileContentResult(content, "application/pdf")
        {
            FileDownloadName = EnsureExtension(fileNameWithoutExtension, ".pdf")
        };
    }

    public static IActionResult Create(
        string? format,
        string fileNameWithoutExtension,
        Func<string> csvFactory,
        Func<byte[]> xlsxFactory,
        Func<byte[]> pdfFactory)
    {
        ArgumentNullException.ThrowIfNull(csvFactory);
        ArgumentNullException.ThrowIfNull(xlsxFactory);
        ArgumentNullException.ThrowIfNull(pdfFactory);

        if (!ReportExportFormats.TryNormalize(format, out string normalized))
        {
            return new BadRequestObjectResult("Export format must be csv, xlsx, or pdf.");
        }

        return normalized switch
        {
            ReportExportFormats.Csv => CreateCsv(fileNameWithoutExtension, csvFactory()),
            ReportExportFormats.Xlsx => CreateXlsx(fileNameWithoutExtension, xlsxFactory()),
            ReportExportFormats.Pdf => CreatePdf(fileNameWithoutExtension, pdfFactory()),
            _ => new BadRequestObjectResult("Export format must be csv, xlsx, or pdf.")
        };
    }

    private static string EnsureExtension(string fileNameWithoutExtension, string extension)
    {
        string name = string.IsNullOrWhiteSpace(fileNameWithoutExtension)
            ? "report"
            : fileNameWithoutExtension.Trim();
        if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }

        return name + extension;
    }
}
