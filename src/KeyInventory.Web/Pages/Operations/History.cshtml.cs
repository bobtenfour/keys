using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class HistoryModel : PageModel
{
    private readonly IListReturnedLoansUseCase _listReturnedLoans;

    public HistoryModel(IListReturnedLoansUseCase listReturnedLoans)
    {
        _listReturnedLoans = listReturnedLoans ?? throw new ArgumentNullException(nameof(listReturnedLoans));
    }

    public IReadOnlyList<LoanListItem> HistoryItems { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        HistoryItems = await _listReturnedLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
