using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Reports;

public sealed class KeyHistoryModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public KeyHistoryModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? KeySearch { get; private set; }

    public string? SelectedKeyNumber { get; private set; }

    public IReadOnlyList<SelectListItem> KeyOptions { get; private set; } = [];

    public IReadOnlyList<KeyHistoryReportRow> Rows { get; private set; } = [];

    public bool KeySelected { get; private set; }

    public async Task OnGetAsync(string? key, string? q, CancellationToken cancellationToken)
    {
        KeySearch = q;
        SelectedKeyNumber = key;
        IReadOnlyList<string> keyNumbers = await _reports.ListKeyNumbersAsync(q, cancellationToken).ConfigureAwait(false);
        KeyOptions = keyNumbers
            .Select(keyNumber => new SelectListItem(
                keyNumber,
                keyNumber,
                string.Equals(keyNumber, SelectedKeyNumber, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        KeySelected = true;
        Rows = await _reports.ListKeyHistoryAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? key, string? format, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return RedirectToPage();
        }

        IReadOnlyList<KeyHistoryReportRow> rows =
            await _reports.ListKeyHistoryAsync(key, cancellationToken).ConfigureAwait(false);
        string filterContext = ReportFilterContext.Key(key);
        return ReportExportResultFactory.Create(
            format,
            $"key-history-{key}",
            () => _reports.FormatKeyHistoryCsv(rows),
            () => _reports.FormatKeyHistoryXlsx(rows, filterContext),
            () => _reports.FormatKeyHistoryPdf(rows, filterContext));
    }
}
