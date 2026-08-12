using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Operations;

public sealed class ReceiveModel : PageModel
{
    private readonly ICompleteReturnUseCase _completeReturn;
    private readonly IOperationalKeyLookupUseCase _lookup;

    public ReceiveModel(ICompleteReturnUseCase completeReturn, IOperationalKeyLookupUseCase lookup)
    {
        _completeReturn = completeReturn ?? throw new ArgumentNullException(nameof(completeReturn));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    [BindProperty]
    public string ReceiveReference { get; set; } = string.Empty;

    [BindProperty]
    public string IssueReference { get; set; } = string.Empty;

    [BindProperty]
    public string ReceivedLocalText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> ActiveIssueOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? issueReference, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(issueReference))
        {
            IssueReference = issueReference;
        }

        await LoadActiveIssuesAsync(cancellationToken).ConfigureAwait(false);
        ReceivedLocalText = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadActiveIssuesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!OperatorLocalTimestamp.TryParseToUtc(ReceivedLocalText, out DateTimeOffset receivedAtUtc, out string? receivedError))
            {
                throw new InvalidOperationException(receivedError ?? "Receive time is invalid.");
            }

            await _completeReturn.ExecuteAsync(ReceiveReference, IssueReference, receivedAtUtc, cancellationToken)
                .ConfigureAwait(false);

            SuccessMessage = $"Key receive completed for issue {IssueReference}.";
            ReceiveReference = string.Empty;
            IssueReference = string.Empty;
            ReceivedLocalText = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow);
            ModelState.Clear();
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
        IReadOnlyList<OperationalLoanDisplay> openItems =
            await _lookup.ListOpenLoansWithHoldersAsync(cancellationToken).ConfigureAwait(false);
        ActiveIssueOptions = openItems
            .Select(item => new SelectListItem(
                $"{PartyHolderDisplayFormatter.FormatKeyCopy(item.KeyNumber, item.MedecoKeyCode)} · {PartyHolderDisplayFormatter.Format(item.HolderFirstName, item.HolderLastName, item.HolderUin)}",
                item.LoanCode,
                string.Equals(item.LoanCode, IssueReference, StringComparison.Ordinal)))
            .ToArray();
    }
}
