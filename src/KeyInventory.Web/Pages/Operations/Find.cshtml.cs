using KeyInventory.Application.Lookup;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class FindModel : PageModel
{
    private readonly IOperationalKeyLookupUseCase _lookup;

    public FindModel(IOperationalKeyLookupUseCase lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public string? Query { get; private set; }

    public IReadOnlyList<KeyLookupResult> Results { get; private set; } = [];

    public bool SearchSubmitted { get; private set; }

    public async Task OnGetAsync(string? q, CancellationToken cancellationToken)
    {
        Query = q;
        SearchSubmitted = !string.IsNullOrWhiteSpace(q);
        if (!SearchSubmitted)
        {
            return;
        }

        Results = await _lookup.SearchKeysAsync(q!, cancellationToken).ConfigureAwait(false);
    }
}
