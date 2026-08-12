using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class KeysModel : PageModel
{
    private readonly IListKeyAssetsUseCase _listKeyAssets;
    private readonly IListOpenLoansUseCase _listOpenLoans;

    public KeysModel(IListKeyAssetsUseCase listKeyAssets, IListOpenLoansUseCase listOpenLoans)
    {
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    public IReadOnlyList<KeyAssetListItem> Keys { get; private set; } = [];
    public HashSet<Guid> IssuedKeyAssetIds { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IssuedKeyAssetIds = openItems.Select(item => item.KeyAssetId).ToHashSet();
    }
}
