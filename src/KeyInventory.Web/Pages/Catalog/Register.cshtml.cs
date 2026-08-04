using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class RegisterModel : PageModel
{
    private readonly ICreateKeyAssetUseCase _createKeyAsset;

    public RegisterModel(ICreateKeyAssetUseCase createKeyAsset)
    {
        _createKeyAsset = createKeyAsset ?? throw new ArgumentNullException(nameof(createKeyAsset));
    }

    [BindProperty]
    public string CatalogKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string TypeCode { get; set; } = string.Empty;

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _createKeyAsset.ExecuteAsync(CatalogKeyCode, TypeCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Key {CatalogKeyCode} was registered.";
            CatalogKeyCode = string.Empty;
            TypeCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }
}
