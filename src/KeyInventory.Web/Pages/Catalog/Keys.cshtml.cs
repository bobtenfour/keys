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

    public KeysModel(
        IConfigurationLifecycleUseCase lifecycle,
        IListKeyAssetsUseCase listKeyAssets)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
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

    public async Task<IActionResult> OnPostActivateAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.ActivateKeyAssetAsync(keyAssetId, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Physical key copy was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.RetireKeyAssetAsync(keyAssetId, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Physical key copy was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostActivatePatternAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.ActivateKeyAccessPatternAsync(keyNumber, cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage = $"KEY # {keyNumber} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetirePatternAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.RetireKeyAccessPatternAsync(keyNumber, cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage = $"KEY # {keyNumber} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
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
