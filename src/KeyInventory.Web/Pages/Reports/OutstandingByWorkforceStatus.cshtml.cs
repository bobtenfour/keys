using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Reports;

public sealed class OutstandingByWorkforceStatusModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public OutstandingByWorkforceStatusModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? StatusFilter { get; private set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; private set; } = [];

    public IReadOnlyList<OutstandingWorkforceKeyReportRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(string? status, CancellationToken cancellationToken)
    {
        StatusFilter = status;
        StatusOptions =
        [
            new SelectListItem("All statuses", string.Empty, string.IsNullOrWhiteSpace(status)),
            new SelectListItem("Active", "Active", string.Equals(status, "Active", StringComparison.Ordinal)),
            new SelectListItem("Terminated", "Terminated", string.Equals(status, "Terminated", StringComparison.Ordinal))
        ];
        Rows = await _reports.ListOutstandingKeysByWorkforceStatusAsync(status, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? status, string? format, CancellationToken cancellationToken)
    {
        IReadOnlyList<OutstandingWorkforceKeyReportRow> rows =
            await _reports.ListOutstandingKeysByWorkforceStatusAsync(status, cancellationToken).ConfigureAwait(false);
        string filterContext = ReportFilterContext.Status(status);
        return ReportExportResultFactory.Create(
            format,
            "outstanding-by-workforce-status",
            () => _reports.FormatOutstandingKeysByWorkforceStatusCsv(rows),
            () => _reports.FormatOutstandingKeysByWorkforceStatusXlsx(rows, filterContext),
            () => _reports.FormatOutstandingKeysByWorkforceStatusPdf(rows, filterContext));
    }
}
