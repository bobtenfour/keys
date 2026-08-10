using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Organizations;

public sealed class AddModel : PageModel
{
    private readonly ICreateOrganizationUseCase _create;

    public AddModel(ICreateOrganizationUseCase create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(OrganizationCode, cancellationToken).ConfigureAwait(false);
            return RedirectToPage("./Index");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
