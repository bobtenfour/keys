using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class KeyTypesModel : PageModel
{
    private readonly IListKeyAssetsUseCase _listKeyAssets;

    public KeyTypesModel(IListKeyAssetsUseCase listKeyAssets)
    {
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
    }

    public IReadOnlyList<KeyTypeSummary> KeyTypes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        KeyTypes = keys
            .GroupBy(key => key.TypeCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => new KeyTypeSummary(group.Key, group.Count(), group.Count(key => key.IsActive)))
            .OrderBy(item => item.TypeCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record KeyTypeSummary(string TypeCode, int KeyCount, int ActiveKeyCount);