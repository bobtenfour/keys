using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class ReceiveModel : PageModel
{
    private const string SuccessTempDataKey = "ReceiveSuccessMessage";

    private readonly ICompleteReturnUseCase _completeReturn;
    private readonly IOperationalKeyLookupUseCase _lookup;
    private readonly ISearchOpenCustodyUseCase _searchOpenCustody;

    public ReceiveModel(
        ICompleteReturnUseCase completeReturn,
        IOperationalKeyLookupUseCase lookup,
        ISearchOpenCustodyUseCase searchOpenCustody)
    {
        _completeReturn = completeReturn ?? throw new ArgumentNullException(nameof(completeReturn));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _searchOpenCustody = searchOpenCustody ?? throw new ArgumentNullException(nameof(searchOpenCustody));
    }

    [BindProperty]
    public string ReceiveReference { get; set; } = string.Empty;

    [BindProperty]
    public string IssueReference { get; set; } = string.Empty;

    [BindProperty]
    public string ReceivedLocalText { get; set; } = string.Empty;

    public OperationalLoanDisplay? SelectedIssue { get; private set; }

    public string SelectedDisplay { get; private set; } = string.Empty;

    public string ClassificationDisplay { get; private set; } = string.Empty;

    public string HolderDisplay { get; private set; } = string.Empty;

    public string IssuedDisplay { get; private set; } = string.Empty;

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? issueReference, CancellationToken cancellationToken)
    {
        if (TempData.TryGetValue(SuccessTempDataKey, out object? success) && success is string text)
        {
            SuccessMessage = text;
        }

        ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);

        string? deepLink = string.IsNullOrWhiteSpace(issueReference) ? null : issueReference.Trim();
        if (!string.IsNullOrWhiteSpace(deepLink) && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            SelectedIssue = await _lookup.FindOpenLoanByLoanCodeAsync(deepLink, cancellationToken)
                .ConfigureAwait(false);
            if (SelectedIssue is not null)
            {
                IssueReference = SelectedIssue.LoanCode;
                PopulateSelectedDisplays(SelectedIssue);
            }
            else
            {
                ErrorMessage = "That active issue was not found or is no longer open.";
            }
        }
    }

    /// <summary>
    /// JSON handler used by the searchable combobox to browse/search open custody.
    /// </summary>
    public async Task<IActionResult> OnGetSearchOpenCustodyAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationalLoanDisplay> matches = await _searchOpenCustody
            .ExecuteAsync(q ?? string.Empty, ISearchOpenCustodyUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(item => new
            {
                loanCode = item.LoanCode,
                keyNumber = item.KeyNumber,
                medecoKeyCode = item.MedecoKeyCode,
                classification = item.Classification.ToString(),
                holder = PartyHolderDisplayFormatter.Format(item.HolderFirstName, item.HolderLastName, item.HolderUin),
                uin = item.HolderUin
            })
            .ToArray();
        return new JsonResult(result);
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
                    PopulateSelectedDisplays(SelectedIssue);
                }
            }

            if (string.IsNullOrWhiteSpace(ReceivedLocalText))
            {
                ReceivedLocalText = OperatorLocalTimestamp.ToOperatorEntryValue(DateTimeOffset.UtcNow);
            }

            return Page();
        }
    }

    private void PopulateSelectedDisplays(OperationalLoanDisplay issue)
    {
        SelectedDisplay = PartyHolderDisplayFormatter.FormatKeyCopy(issue.KeyNumber, issue.MedecoKeyCode);
        ClassificationDisplay = issue.Classification.ToString();
        HolderDisplay = PartyHolderDisplayFormatter.Format(issue.HolderFirstName, issue.HolderLastName, issue.HolderUin);
        IssuedDisplay = OperatorTimestampFormatter.ToAbsoluteDisplay(issue.IssuedAtUtc);
    }
}
