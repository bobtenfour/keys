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
    public string KeyNumber { get; set; } = string.Empty;

    [BindProperty]
    public string MedecoKeyCode { get; set; } = string.Empty;

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
            await _createKeyAsset.ExecuteAsync(KeyNumber, MedecoKeyCode, TypeCode, cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage =
                $"Physical copy MEDECO {MedecoKeyCode} was registered under KEY # {KeyNumber}.";
            KeyNumber = string.Empty;
            MedecoKeyCode = string.Empty;
            TypeCode = string.Empty;
            ModelState.Clear();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }
}
