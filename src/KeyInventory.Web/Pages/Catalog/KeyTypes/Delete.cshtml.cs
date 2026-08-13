using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog.KeyTypes;

public sealed class DeleteModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public DeleteModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    [BindProperty(SupportsGet = true)]
    public string TypeCode { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public KeyTypeLifecycleItem? Item { get; private set; }

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
                ?? "This key type can no longer be deleted because it is in use. Retire it instead to preserve its history.";
            return RedirectToPage("/Catalog/KeyTypes");
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
            await _lifecycle.DeleteKeyTypeAsync(TypeCode, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Key type \"{TypeCode}\" was deleted.";
            return RedirectToPage("/Catalog/KeyTypes");
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = FormatDeleteError(exception);
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false)
                || Item is null
                || !Item.Capabilities.CanDelete)
            {
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage("/Catalog/KeyTypes");
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
        if (string.IsNullOrWhiteSpace(TypeCode))
        {
            return false;
        }

        string code = TypeCode.Trim();
        Item = (await _lifecycle.ListKeyTypesAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item =>
                string.Equals(item.TypeCode, code, StringComparison.OrdinalIgnoreCase));
        if (Item is not null)
        {
            TypeCode = Item.TypeCode;
        }

        return Item is not null;
    }

    private static string FormatDeleteError(InvalidOperationException exception)
    {
        if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return exception.Message;
        }

        return "This key type can no longer be deleted because it is in use. Retire it instead to preserve its history.";
    }
}
