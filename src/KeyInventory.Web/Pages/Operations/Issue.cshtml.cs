using System.Globalization;
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

    public IssueModel(
        IIssueLoanUseCase issueLoan,
        IListKeyAssetsUseCase listKeyAssets,
        IListOpenLoansUseCase listOpenLoans)
    {
        _issueLoan = issueLoan ?? throw new ArgumentNullException(nameof(issueLoan));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    [BindProperty]
    public string IssueReference { get; set; } = string.Empty;

    [BindProperty]
    public string CatalogKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string BorrowerPartyReference { get; set; } = string.Empty;

    [BindProperty]
    public string IssuedAtUtcText { get; set; } = string.Empty;

    [BindProperty]
    public string DueAtUtcText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> KeyOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadKeysAsync(cancellationToken).ConfigureAwait(false);
        IssuedAtUtcText = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        DueAtUtcText = DateTimeOffset.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadKeysAsync(cancellationToken).ConfigureAwait(false);

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
                    BorrowerPartyReference,
                    issuedAtUtc,
                    dueAtUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            SuccessMessage = $"Key {CatalogKeyCode} was issued.";
            IssueReference = string.Empty;
            BorrowerPartyReference = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private async Task LoadKeysAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> issued = openItems.Select(item => item.CatalogKeyCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        KeyOptions = keys
            .Where(key => key.IsActive && !issued.Contains(key.CatalogKeyCode))
            .Select(key => new SelectListItem(key.CatalogKeyCode, key.CatalogKeyCode))
            .ToArray();
    }
}
