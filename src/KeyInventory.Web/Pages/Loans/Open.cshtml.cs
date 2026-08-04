using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Loans;

public sealed class OpenModel : PageModel
{
    private readonly IListOpenLoansUseCase _listOpenLoans;

    public OpenModel(IListOpenLoansUseCase listOpenLoans)
    {
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    public IReadOnlyList<LoanListItem> Loans { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Loans = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
