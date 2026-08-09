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

    public async Task<IActionResult> OnGetExportAsync(string? key, string? format, CancellationToken cancellationToken)
    {
        IReadOnlyList<OverdueKeyReportRow> rows =
            await _reports.ListOverdueKeysAsync(DateTimeOffset.UtcNow, key, cancellationToken).ConfigureAwait(false);
        string filterContext = ReportFilterContext.Key(key);
        return ReportExportResultFactory.Create(
            format,
            "overdue-keys",
            () => _reports.FormatOverdueKeysCsv(rows),
            () => _reports.FormatOverdueKeysXlsx(rows, filterContext),
            () => _reports.FormatOverdueKeysPdf(rows, filterContext));
    }
}
