using KeyInventory.Application.Lookup;
using KeyInventory.Application.Reports;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Reports;

public sealed class KeysByWorkforceMemberModel : PageModel
{
    private readonly IOperationalReportsUseCase _reports;

    public KeysByWorkforceMemberModel(IOperationalReportsUseCase reports)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public string? MemberSearch { get; private set; }

    public string? SelectedMemberCode { get; private set; }

    public IReadOnlyList<SelectListItem> MemberOptions { get; private set; } = [];

    public KeysByWorkforceMemberReport? Report { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? member, string? q, CancellationToken cancellationToken)
    {
        MemberSearch = q;
        SelectedMemberCode = member;
        await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(member))
        {
            return;
        }

        try
        {
            Report = await _reports.GetKeysByWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
            if (Report is null)
            {
                ErrorMessage = "The selected workforce member was not found.";
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }
    }

    public async Task<IActionResult> OnGetExportAsync(string? member, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(member))
        {
            return RedirectToPage(new { member, q = MemberSearch });
        }

        KeysByWorkforceMemberReport? report =
            await _reports.GetKeysByWorkforceMemberAsync(member, cancellationToken).ConfigureAwait(false);
        if (report is null)
        {
            return NotFound();
        }

        return ReportCsvResultFactory.Create(
            $"keys-by-member-{member}.csv",
            _reports.FormatKeysByWorkforceMemberCsv(report));
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkforceMemberReportOption> options =
            await _reports.ListWorkforceMemberOptionsAsync(MemberSearch, cancellationToken).ConfigureAwait(false);
        MemberOptions = options
            .Select(item => new SelectListItem(
                $"{PartyHolderDisplayFormatter.Format(item.FirstName, item.LastName, item.Uin)} · {item.WorkforceMemberCode} · {item.Status}",
                item.WorkforceMemberCode,
                string.Equals(item.WorkforceMemberCode, SelectedMemberCode, StringComparison.Ordinal)))
            .ToArray();
    }
}
