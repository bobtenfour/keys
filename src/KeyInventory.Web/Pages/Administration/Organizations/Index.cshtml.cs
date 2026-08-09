using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Organizations;

public sealed class IndexModel : PageModel
{
    private readonly ICreateOrganizationUseCase _create;
    private readonly IListOrganizationsUseCase _list;
    private readonly IActivateOrganizationUseCase _activate;
    private readonly IRetireOrganizationUseCase _retire;

    public IndexModel(
        ICreateOrganizationUseCase create,
        IListOrganizationsUseCase list,
        IActivateOrganizationUseCase activate,
        IRetireOrganizationUseCase retire)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
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

    public async Task<IActionResult> OnPostActivateAsync(string organizationCode, CancellationToken cancellationToken)
    {
        try
        {
            await _activate.ExecuteAsync(organizationCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Organization {organizationCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Organizations = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string organizationCode, CancellationToken cancellationToken)
    {
        try
        {
            await _retire.ExecuteAsync(organizationCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Organization {organizationCode} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Organizations = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
