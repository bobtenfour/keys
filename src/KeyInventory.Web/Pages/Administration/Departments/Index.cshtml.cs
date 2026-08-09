using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class IndexModel : PageModel
{
    private readonly ICreateDepartmentUseCase _create;
    private readonly IListDepartmentsUseCase _list;
    private readonly IListOrganizationsUseCase _organizations;
    private readonly IActivateDepartmentUseCase _activate;
    private readonly IRetireDepartmentUseCase _retire;

    public IndexModel(
        ICreateDepartmentUseCase create,
        IListDepartmentsUseCase list,
        IListOrganizationsUseCase organizations,
        IActivateDepartmentUseCase activate,
        IRetireDepartmentUseCase retire)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _organizations = organizations ?? throw new ArgumentNullException(nameof(organizations));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
    }

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    public IReadOnlyList<DepartmentListItem> Departments { get; private set; } = [];

    public IReadOnlyList<SelectListItem> OrganizationOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

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
            SuccessMessage = $"Department {DepartmentCode} was created.";
            DepartmentCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostActivateAsync(
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await _activate.ExecuteAsync(organizationCode, departmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Department {departmentCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await _retire.ExecuteAsync(organizationCode, departmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Department {departmentCode} was retired.";
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
        Departments = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        OrganizationOptions = (await _organizations.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(item.OrganizationCode, item.OrganizationCode))
            .ToArray();
    }
}
