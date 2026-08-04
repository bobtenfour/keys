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
    public HashSet<string> IssuedKeyCodes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IssuedKeyCodes = openItems.Select(item => item.CatalogKeyCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
