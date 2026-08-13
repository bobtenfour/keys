using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog.Keys;

public sealed class DeletePatternModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public DeletePatternModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    [BindProperty(SupportsGet = true)]
    public string KeyNumber { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public KeyAccessPatternLifecycleItem? Item { get; private set; }

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
                ?? "This KEY # can no longer be deleted because it still has physical copies or room assignments.";
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
            await _lifecycle.DeleteKeyAccessPatternAsync(KeyNumber, cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"KEY # \"{KeyNumber}\" was deleted.";
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
        if (string.IsNullOrWhiteSpace(KeyNumber))
        {
            return false;
        }

        string keyNumber = KeyNumber.Trim();
        Item = (await _lifecycle.ListKeyAccessPatternsAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item =>
                string.Equals(item.KeyNumber, keyNumber, StringComparison.OrdinalIgnoreCase));
        if (Item is not null)
        {
            KeyNumber = Item.KeyNumber;
        }

        return Item is not null;
    }

    private static string FormatDeleteError(InvalidOperationException exception)
    {
        if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return exception.Message;
        }

        return "This KEY # can no longer be deleted because it still has physical copies or room assignments. Remove those relationships or retire it to preserve history.";
    }
}
