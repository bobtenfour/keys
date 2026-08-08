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

    public string? SelectedKeyCode { get; private set; }

    public IReadOnlyList<SelectListItem> KeyOptions { get; private set; } = [];

    public IReadOnlyList<KeyHistoryReportRow> Rows { get; private set; } = [];

    public bool KeySelected { get; private set; }

    public async Task OnGetAsync(string? key, string? q, CancellationToken cancellationToken)
    {
        KeySearch = q;
        SelectedKeyCode = key;
        IReadOnlyList<string> codes = await _reports.ListCatalogKeyCodesAsync(q, cancellationToken).ConfigureAwait(false);
        KeyOptions = codes
            .Select(code => new SelectListItem(
                code,
                code,
                string.Equals(code, SelectedKeyCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        KeySelected = true;
        Rows = await _reports.ListKeyHistoryAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return RedirectToPage();
        }

        IReadOnlyList<KeyHistoryReportRow> rows =
            await _reports.ListKeyHistoryAsync(key, cancellationToken).ConfigureAwait(false);
        return ReportCsvResultFactory.Create($"key-history-{key}.csv", _reports.FormatKeyHistoryCsv(rows));
    }
}
