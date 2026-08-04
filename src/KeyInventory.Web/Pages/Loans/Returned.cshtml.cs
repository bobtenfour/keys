using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Loans;

public sealed class ReturnedModel : PageModel
{
    private readonly IListReturnedLoansUseCase _listReturnedLoans;

    public ReturnedModel(IListReturnedLoansUseCase listReturnedLoans)
    {
        _listReturnedLoans = listReturnedLoans ?? throw new ArgumentNullException(nameof(listReturnedLoans));
    }

    public IReadOnlyList<LoanListItem> Loans { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Loans = await _listReturnedLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
