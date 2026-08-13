using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class IndexModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public IndexModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    public IReadOnlyList<DepartmentLifecycleItem> Departments { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        Departments = await _lifecycle.ListDepartmentsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.ActivateDepartmentAsync(departmentId, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Department was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Departments = await _lifecycle.ListDepartmentsAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.RetireDepartmentAsync(departmentId, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Department was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Departments = await _lifecycle.ListDepartmentsAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
