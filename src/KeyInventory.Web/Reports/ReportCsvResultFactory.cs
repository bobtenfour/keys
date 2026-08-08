using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace KeyInventory.Web.Reports;

internal static class ReportCsvResultFactory
{
    public static FileContentResult Create(string fileName, string csvContent)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent ?? string.Empty);
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = fileName
        };
    }
}
