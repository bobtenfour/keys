using KeyInventory.Application.Lookup;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class HistoryModel : PageModel
{
    private readonly IOperationalKeyLookupUseCase _lookup;

    public HistoryModel(IOperationalKeyLookupUseCase lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public IReadOnlyList<OperationalLoanDisplay> HistoryItems { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        HistoryItems = await _lookup.ListReturnedLoansWithHoldersAsync(cancellationToken).ConfigureAwait(false);
    }
}
