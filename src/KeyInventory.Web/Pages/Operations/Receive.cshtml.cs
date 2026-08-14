using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class ReceiveModel : PageModel
{
    private const string SuccessTempDataKey = "ReceiveSuccessMessage";
    private const string SelectedIssueTempDataKey = "ReceiveSelectedIssueReference";

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

    [BindProperty]
    public string IssueSearchText { get; set; } = string.Empty;

    public IReadOnlyList<OperationalLoanDisplay> IssueMatches { get; private set; } = [];

    public bool IssueSearchPerformed { get; private set; }

    public OperationalLoanDisplay? SelectedIssue { get; private set; }

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? issueReference, CancellationToken cancellationToken)
    {
        if (TempData.TryGetValue(SuccessTempDataKey, out object? success) && success is string text)
        {
            SuccessMessage = text;
        }

        ReceiveReference = string.Empty;
        ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);

        // Deliberate deep-link from Active Loans / Member Keys only. Never auto-pick first/only issue.
        string? deepLink = string.IsNullOrWhiteSpace(issueReference) ? null : issueReference.Trim();
        string? selectedFromSession = TempData.Peek(SelectedIssueTempDataKey) as string;
        string? selectedCode = deepLink ?? selectedFromSession;
        if (!string.IsNullOrWhiteSpace(selectedCode) && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            SelectedIssue = await _lookup.FindOpenLoanByLoanCodeAsync(selectedCode, cancellationToken)
                .ConfigureAwait(false);
            if (SelectedIssue is not null)
            {
                IssueReference = SelectedIssue.LoanCode;
                TempData[SelectedIssueTempDataKey] = SelectedIssue.LoanCode;
            }
            else
            {
                TempData.Remove(SelectedIssueTempDataKey);
                if (!string.IsNullOrWhiteSpace(deepLink))
                {
                    ErrorMessage = "That active issue was not found or is no longer open.";
                }
            }
        }
    }

    public async Task<IActionResult> OnPostSearchIssuesAsync(CancellationToken cancellationToken)
    {
        TempData.Remove(SelectedIssueTempDataKey);
        SelectedIssue = null;
        IssueReference = string.Empty;
        IssueSearchPerformed = true;
        ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
        IssueMatches = await _lookup
            .SearchOpenLoansWithHoldersAsync(
                IssueSearchText,
                IOperationalKeyLookupUseCase.DefaultOpenLoanSearchMaxResults,
                cancellationToken)
            .ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostSelectIssueAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IssueReference))
        {
            ErrorMessage = "Select the active issue being returned.";
            ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
            return Page();
        }

        OperationalLoanDisplay? match = await _lookup
            .FindOpenLoanByLoanCodeAsync(IssueReference, cancellationToken)
            .ConfigureAwait(false);
        if (match is null)
        {
            ErrorMessage = "That active issue was not found or is no longer open.";
            ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
            return Page();
        }

        TempData[SelectedIssueTempDataKey] = match.LoanCode;
        return RedirectToPage();
    }

    public IActionResult OnPostClearIssue()
    {
        TempData.Remove(SelectedIssueTempDataKey);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IssueReference))
            {
                throw new InvalidOperationException("Select the active issue being returned.");
            }

            SelectedIssue = await _lookup.FindOpenLoanByLoanCodeAsync(IssueReference, cancellationToken)
                .ConfigureAwait(false);
            if (SelectedIssue is null)
            {
                throw new InvalidOperationException("That active issue was not found or is no longer open.");
            }

            if (!OperatorLocalTimestamp.TryParseToUtc(ReceivedLocalText, out DateTimeOffset receivedAtUtc, out string? receivedError))
            {
                throw new InvalidOperationException(receivedError ?? "Receive time is invalid.");
            }

            string selectedLabel =
                $"{PartyHolderDisplayFormatter.FormatKeyCopy(SelectedIssue.KeyNumber, SelectedIssue.MedecoKeyCode)} · {PartyHolderDisplayFormatter.Format(SelectedIssue.HolderFirstName, SelectedIssue.HolderLastName, SelectedIssue.HolderUin)}";

            await _completeReturn.ExecuteAsync(ReceiveReference, IssueReference, receivedAtUtc, cancellationToken)
                .ConfigureAwait(false);

            TempData.Remove(SelectedIssueTempDataKey);
            TempData[SuccessTempDataKey] = $"Received {selectedLabel}.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            if (!string.IsNullOrWhiteSpace(IssueReference))
            {
                SelectedIssue ??= await _lookup.FindOpenLoanByLoanCodeAsync(IssueReference, cancellationToken)
                    .ConfigureAwait(false);
                if (SelectedIssue is not null)
                {
                    TempData[SelectedIssueTempDataKey] = SelectedIssue.LoanCode;
                }
            }

            if (string.IsNullOrWhiteSpace(ReceivedLocalText))
            {
                ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
            }

            return Page();
        }
    }
}
