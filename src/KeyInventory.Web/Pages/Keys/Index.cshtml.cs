using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Keys;

public sealed class IndexModel : PageModel
{
    private readonly IListKeyAssetsUseCase _listKeyAssets;

    public IndexModel(IListKeyAssetsUseCase listKeyAssets)
    {
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
    }

    public IReadOnlyList<KeyAssetListItem> Keys { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }
}
