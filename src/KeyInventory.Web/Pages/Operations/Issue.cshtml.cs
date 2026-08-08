using System.Globalization;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Operations;

public sealed class IssueModel : PageModel
{
    private readonly IIssueLoanUseCase _issueLoan;
    private readonly IListKeyAssetsUseCase _listKeyAssets;
    private readonly IListOpenLoansUseCase _listOpenLoans;
    private readonly IOperationalKeyLookupUseCase _lookup;

    public IssueModel(
        IIssueLoanUseCase issueLoan,
        IListKeyAssetsUseCase listKeyAssets,
        IListOpenLoansUseCase listOpenLoans,
        IOperationalKeyLookupUseCase lookup)
    {
        _issueLoan = issueLoan ?? throw new ArgumentNullException(nameof(issueLoan));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    [BindProperty]
    public string IssueReference { get; set; } = string.Empty;

    [BindProperty]
    public string CatalogKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string JustificationKind { get; set; } = "Department";

    [BindProperty]
    public string JustificationCode { get; set; } = string.Empty;

    [BindProperty]
    public string IssuedAtUtcText { get; set; } = string.Empty;

    [BindProperty]
    public string DueAtUtcText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> KeyOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> WorkforceMemberOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? catalogKeyCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(catalogKeyCode))
        {
            CatalogKeyCode = catalogKeyCode;
        }

        await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);
        IssuedAtUtcText = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        DueAtUtcText = DateTimeOffset.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!DateTimeOffset.TryParse(IssuedAtUtcText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset issuedAtUtc))
            {
                throw new InvalidOperationException("Issue time must be a valid UTC timestamp.");
            }

            if (!DateTimeOffset.TryParse(DueAtUtcText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dueAtUtc))
            {
                throw new InvalidOperationException("Due time must be a valid UTC timestamp.");
            }

            await _issueLoan.ExecuteAsync(
                    IssueReference,
                    CatalogKeyCode,
                    WorkforceMemberCode,
                    JustificationKind,
                    JustificationCode,
                    issuedAtUtc,
                    dueAtUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            SuccessMessage = $"Key {CatalogKeyCode} was issued.";
            IssueReference = string.Empty;
            WorkforceMemberCode = string.Empty;
            JustificationCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> issued = openItems.Select(item => item.CatalogKeyCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        KeyOptions = keys
            .Where(key => key.IsActive && !issued.Contains(key.CatalogKeyCode))
            .Select(key => new SelectListItem(
                key.CatalogKeyCode,
                key.CatalogKeyCode,
                string.Equals(key.CatalogKeyCode, CatalogKeyCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        IReadOnlyList<WorkforceMemberIdentityDisplay> members = await _lookup
            .ListActiveWorkforceMembersWithIdentityAsync(cancellationToken)
            .ConfigureAwait(false);
        WorkforceMemberOptions = members
            .Select(member => new SelectListItem(
                $"{PartyHolderDisplayFormatter.Format(member.FirstName, member.LastName, member.Uin)} · {member.WorkforceMemberCode}",
                member.WorkforceMemberCode,
                string.Equals(member.WorkforceMemberCode, WorkforceMemberCode, StringComparison.Ordinal)))
            .ToArray();
    }
}
