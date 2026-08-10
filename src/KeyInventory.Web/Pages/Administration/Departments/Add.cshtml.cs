using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class AddModel : PageModel
{
    private readonly ICreateDepartmentUseCase _create;
    private readonly IListOrganizationsUseCase _organizations;

    public AddModel(ICreateDepartmentUseCase create, IListOrganizationsUseCase organizations)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
    }

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> OrganizationOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(OrganizationCode, DepartmentCode, cancellationToken).ConfigureAwait(false);
            return RedirectToPage("./Index");
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
        OrganizationOptions = (await _organizations.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(
                item.OrganizationCode,
                item.OrganizationCode,
                string.Equals(item.OrganizationCode, OrganizationCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}
