using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Buildings;

public sealed class AddModel : PageModel
{
    private readonly ICreateBuildingUseCase _create;

    public AddModel(ICreateBuildingUseCase create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    [BindProperty]
    public string BuildingCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(BuildingCode, cancellationToken).ConfigureAwait(false);
            return RedirectToPage("./Index");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
