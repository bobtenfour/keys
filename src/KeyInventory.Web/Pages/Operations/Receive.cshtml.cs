using System.Globalization;
using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Operations;

public sealed class ReceiveModel : PageModel
{
    private readonly ICompleteReturnUseCase _completeReturn;
    private readonly IListOpenLoansUseCase _listOpenLoans;

    public ReceiveModel(ICompleteReturnUseCase completeReturn, IListOpenLoansUseCase listOpenLoans)
    {
        _completeReturn = completeReturn ?? throw new ArgumentNullException(nameof(completeReturn));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    [BindProperty]
    public string ReceiveReference { get; set; } = string.Empty;

    [BindProperty]
    public string IssueReference { get; set; } = string.Empty;

    [BindProperty]
    public string ReceivedAtUtcText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> ActiveIssueOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadActiveIssuesAsync(cancellationToken).ConfigureAwait(false);
        ReceivedAtUtcText = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadActiveIssuesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!DateTimeOffset.TryParse(ReceivedAtUtcText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset receivedAtUtc))
            {
                throw new InvalidOperationException("Receive time must be a valid UTC timestamp.");
            }

            await _completeReturn.ExecuteAsync(ReceiveReference, IssueReference, receivedAtUtc, cancellationToken)
                .ConfigureAwait(false);

            SuccessMessage = $"Key receive completed for issue {IssueReference}.";
            ReceiveReference = string.Empty;
            await LoadActiveIssuesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private async Task LoadActiveIssuesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        ActiveIssueOptions = openItems
            .Select(item => new SelectListItem(
                $"{item.CatalogKeyCode} · {item.BorrowerPartyReference}",
                item.LoanCode))
            .ToArray();
    }
}
