using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Reports;

public sealed class CurrentKeyHoldersModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public CurrentKeyHoldersModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? KeyFilter { get; private set; }

    public IReadOnlyList<CurrentKeyHolderReportRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(string? key, CancellationToken cancellationToken)
    {
        KeyFilter = key;
        Rows = await _reports.ListCurrentKeyHoldersAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? key, CancellationToken cancellationToken)
    {
        IReadOnlyList<CurrentKeyHolderReportRow> rows =
            await _reports.ListCurrentKeyHoldersAsync(key, cancellationToken).ConfigureAwait(false);
        return ReportCsvResultFactory.Create("current-key-holders.csv", _reports.FormatCurrentKeyHoldersCsv(rows));
    }
}
