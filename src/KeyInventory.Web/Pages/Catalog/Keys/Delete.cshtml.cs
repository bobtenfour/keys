using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog.Keys;

public sealed class DeleteModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public DeleteModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    [BindProperty(SupportsGet = true)]
    public Guid KeyAssetId { get; set; }

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public KeyAssetLifecycleItem? Item { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!Item!.Capabilities.CanDelete)
        {
            TempData["ErrorMessage"] = Item.Capabilities.DeleteBlockedReason
                ?? "This physical key copy can no longer be deleted because it has loan history. Retire it instead to preserve its history.";
            return RedirectToPage("/Catalog/Keys");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!ConfirmDelete)
        {
            ErrorMessage = "Confirm deletion before continuing.";
            return Page();
        }

        try
        {
            string label = $"{Item!.KeyNumber} / {Item.MedecoKeyCode}";
            await _lifecycle.DeleteKeyAssetAsync(KeyAssetId, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Physical key copy \"{label}\" was deleted.";
            return RedirectToPage("/Catalog/Keys");
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = FormatDeleteError(exception);
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false)
                || Item is null
                || !Item.Capabilities.CanDelete)
            {
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage("/Catalog/Keys");
            }

            return Page();
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        if (KeyAssetId == Guid.Empty)
        {
            return false;
        }

        Item = (await _lifecycle.ListKeyAssetsAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => item.KeyAssetId == KeyAssetId);
        return Item is not null;
    }

    private static string FormatDeleteError(InvalidOperationException exception)
    {
        if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return exception.Message;
        }

        return "This physical key copy can no longer be deleted because it has loan history. Retire it instead to preserve its history.";
    }
}
