using KeyInventory.Application.Lookup;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages;

public sealed class SearchModel : PageModel
{
    private readonly IGlobalOperatorSearchUseCase _search;

    public SearchModel(IGlobalOperatorSearchUseCase search)
    {
        _search = search ?? throw new ArgumentNullException(nameof(search));
    }

    public string? Query { get; private set; }

    public bool SearchSubmitted { get; private set; }

    public GlobalOperatorSearchResult Results { get; private set; } =
        new(string.Empty, [], [], [], []);

    public async Task OnGetAsync(string? q, CancellationToken cancellationToken)
    {
        Query = q;
        SearchSubmitted = !string.IsNullOrWhiteSpace(q);
        if (!SearchSubmitted)
        {
            return;
        }

        Results = await _search.SearchAsync(q!, cancellationToken).ConfigureAwait(false);
    }
}
