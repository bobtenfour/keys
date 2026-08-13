using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class KeyTypesModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public KeyTypesModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    public IReadOnlyList<KeyTypeLifecycleItem> KeyTypes { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        KeyTypes = await _lifecycle.ListKeyTypesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostActivateAsync(string typeCode, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.ActivateKeyTypeAsync(typeCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Key type {typeCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        KeyTypes = await _lifecycle.ListKeyTypesAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string typeCode, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.RetireKeyTypeAsync(typeCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Key type {typeCode} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        KeyTypes = await _lifecycle.ListKeyTypesAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
