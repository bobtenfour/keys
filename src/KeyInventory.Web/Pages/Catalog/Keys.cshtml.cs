using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class KeysModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;
    private readonly IListKeyAssetsUseCase _listKeyAssets;
    private readonly IMarkKeyAssetLostUseCase _markLost;
    private readonly IDestroyKeyAssetUseCase _destroy;

    public KeysModel(
        IConfigurationLifecycleUseCase lifecycle,
        IListKeyAssetsUseCase listKeyAssets,
        IMarkKeyAssetLostUseCase markLost,
        IDestroyKeyAssetUseCase destroy)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _markLost = markLost ?? throw new ArgumentNullException(nameof(markLost));
        _destroy = destroy ?? throw new ArgumentNullException(nameof(destroy));
    }

    public IReadOnlyList<KeyAssetLifecycleItem> Keys { get; private set; } = [];

    public IReadOnlyList<KeyAccessPatternLifecycleItem> AccessPatterns { get; private set; } = [];

    public IReadOnlyDictionary<Guid, IReadOnlyList<KeyOpenedRoomItem>> OpenedRoomsByAssetId
    {
        get;
        private set;
    } = new Dictionary<Guid, IReadOnlyList<KeyOpenedRoomItem>>();

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostMarkLostAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await _markLost.ExecuteAsync(keyAssetId, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "Key was marked Lost.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDestroyAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await _destroy.ExecuteAsync(keyAssetId, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "Key was Destroyed.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostActivatePatternAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.ActivateKeyAccessPatternAsync(keyNumber, cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"KEY # {keyNumber} was activated.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostRetirePatternAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.RetireKeyAccessPatternAsync(keyNumber, cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"KEY # {keyNumber} was retired.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToPage();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Keys = await _lifecycle.ListKeyAssetsAsync(cancellationToken).ConfigureAwait(false);
        AccessPatterns = await _lifecycle.ListKeyAccessPatternsAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Guid, IReadOnlyList<KeyOpenedRoomItem>> rooms = new();
        foreach (KeyAssetListItem asset in await _listKeyAssets.ExecuteAsync(cancellationToken)
                     .ConfigureAwait(false))
        {
            rooms[asset.KeyAssetId] = asset.OpenedRooms;
        }

        OpenedRoomsByAssetId = rooms;
    }
}
