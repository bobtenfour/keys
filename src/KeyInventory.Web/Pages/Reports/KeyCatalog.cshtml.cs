using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Reports;

public sealed class KeyCatalogModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public KeyCatalogModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? KeyFilter { get; private set; }

    public IReadOnlyList<KeyCatalogReportRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(string? key, CancellationToken cancellationToken)
    {
        KeyFilter = key;
        Rows = await _reports.ListKeyCatalogReportAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? key, CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyCatalogReportRow> rows =
            await _reports.ListKeyCatalogReportAsync(key, cancellationToken).ConfigureAwait(false);
        return ReportCsvResultFactory.Create("key-catalog.csv", _reports.FormatKeyCatalogCsv(rows));
    }
}
