using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class IndexModel : PageModel
{
    private readonly IListDepartmentsUseCase _list;
    private readonly IActivateDepartmentUseCase _activate;
    private readonly IRetireDepartmentUseCase _retire;

    public IndexModel(
        IListDepartmentsUseCase list,
        IActivateDepartmentUseCase activate,
        IRetireDepartmentUseCase retire)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
    }

    public IReadOnlyList<DepartmentListItem> Departments { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        Departments = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostActivateAsync(string departmentCode, CancellationToken cancellationToken)
    {
        try
        {
            await _activate.ExecuteAsync(departmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Department {departmentCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Departments = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string departmentCode, CancellationToken cancellationToken)
    {
        try
        {
            await _retire.ExecuteAsync(departmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Department {departmentCode} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Departments = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
