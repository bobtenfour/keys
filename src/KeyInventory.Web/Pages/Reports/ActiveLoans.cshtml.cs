using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Reports;

public sealed class ActiveLoansModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public ActiveLoansModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? KeyFilter { get; private set; }

    public IReadOnlyList<ActiveLoanReportRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(string? key, CancellationToken cancellationToken)
    {
        KeyFilter = key;
        Rows = await _reports.ListActiveLoansReportAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? key, CancellationToken cancellationToken)
    {
        IReadOnlyList<ActiveLoanReportRow> rows =
            await _reports.ListActiveLoansReportAsync(key, cancellationToken).ConfigureAwait(false);
        return ReportCsvResultFactory.Create("active-loans.csv", _reports.FormatActiveLoansCsv(rows));
    }
}
