using KeyInventory.Application.Lookup;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class ActiveModel : PageModel
{
    private readonly IOperationalKeyLookupUseCase _lookup;

    public ActiveModel(IOperationalKeyLookupUseCase lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public IReadOnlyList<OperationalLoanDisplay> ActiveIssues { get; private set; } = [];

    public DateTimeOffset UtcNow { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        UtcNow = DateTimeOffset.UtcNow;
        ActiveIssues = await _lookup.ListOpenLoansWithHoldersAsync(cancellationToken).ConfigureAwait(false);
    }
}
