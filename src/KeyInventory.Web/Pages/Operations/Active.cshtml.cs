using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class ActiveModel : PageModel
{
    private readonly IListOpenLoansUseCase _listOpenLoans;

    public ActiveModel(IListOpenLoansUseCase listOpenLoans)
    {
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    public IReadOnlyList<LoanListItem> ActiveIssues { get; private set; } = [];

    public DateTimeOffset UtcNow { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        UtcNow = DateTimeOffset.UtcNow;
        ActiveIssues = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
