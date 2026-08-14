using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Catalog.KeyTypes;

public sealed class AddModel : PageModel
{
    private readonly ICreateKeyTypeUseCase _create;

    public AddModel(ICreateKeyTypeUseCase create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    [BindProperty]
    public string TypeCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(TypeCode, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Key Type {TypeCode.Trim()} was created.";
            return RedirectToPage("/Catalog/KeyTypes");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
