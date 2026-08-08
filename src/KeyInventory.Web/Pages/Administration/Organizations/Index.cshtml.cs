using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Organizations;

public sealed class IndexModel : PageModel
{
    private readonly ICreateOrganizationUseCase _create;
    private readonly IListOrganizationsUseCase _list;

    public IndexModel(ICreateOrganizationUseCase create, IListOrganizationsUseCase list)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    public IReadOnlyList<OrganizationListItem> Organizations { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Organizations = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(OrganizationCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Organization {OrganizationCode} was created.";
            OrganizationCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Organizations = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
