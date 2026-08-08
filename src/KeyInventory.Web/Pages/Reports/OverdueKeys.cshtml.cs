using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Reports;

public sealed class OverdueKeysModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public OverdueKeysModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? KeyFilter { get; private set; }

    public IReadOnlyList<OverdueKeyReportRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(string? key, CancellationToken cancellationToken)
    {
        KeyFilter = key;
        Rows = await _reports.ListOverdueKeysAsync(DateTimeOffset.UtcNow, key, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? key, CancellationToken cancellationToken)
    {
        IReadOnlyList<OverdueKeyReportRow> rows =
            await _reports.ListOverdueKeysAsync(DateTimeOffset.UtcNow, key, cancellationToken).ConfigureAwait(false);
        return ReportCsvResultFactory.Create("overdue-keys.csv", _reports.FormatOverdueKeysCsv(rows));
    }
}
