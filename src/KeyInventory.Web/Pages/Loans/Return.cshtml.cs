using System.Globalization;
using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Loans;

public sealed class ReturnModel : PageModel
{
    private readonly ICompleteReturnUseCase _completeReturn;
    private readonly IListOpenLoansUseCase _listOpenLoans;

    public ReturnModel(ICompleteReturnUseCase completeReturn, IListOpenLoansUseCase listOpenLoans)
    {
        _completeReturn = completeReturn ?? throw new ArgumentNullException(nameof(completeReturn));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    [BindProperty]
    public string ReturnCode { get; set; } = string.Empty;

    [BindProperty]
    public string LoanCode { get; set; } = string.Empty;

    [BindProperty]
    public string ReturnedAtUtcText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> OpenLoanOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOpenLoansAsync(cancellationToken).ConfigureAwait(false);
        ReturnedAtUtcText = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOpenLoansAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!DateTimeOffset.TryParse(ReturnedAtUtcText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset returnedAtUtc))
            {
                throw new InvalidOperationException("Return time must be a valid UTC timestamp.");
            }

            await _completeReturn.ExecuteAsync(ReturnCode, LoanCode, returnedAtUtc, cancellationToken)
                .ConfigureAwait(false);

            SuccessMessage = $"Return {ReturnCode} was completed for loan {LoanCode}.";
            ReturnCode = string.Empty;
            await LoadOpenLoansAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private async Task LoadOpenLoansAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<LoanListItem> loans = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        OpenLoanOptions = loans
            .Select(loan => new SelectListItem(loan.LoanCode, loan.LoanCode))
            .ToArray();
    }
}
