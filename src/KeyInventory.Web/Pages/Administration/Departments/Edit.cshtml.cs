using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class EditModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;
    private readonly IUpdateDepartmentCodeUseCase _updateCode;

    public EditModel(
        IConfigurationLifecycleUseCase lifecycle,
        IUpdateDepartmentCodeUseCase updateCode)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _updateCode = updateCode ?? throw new ArgumentNullException(nameof(updateCode));
    }

    [BindProperty(SupportsGet = true)]
    public Guid DepartmentId { get; set; }

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    public DepartmentLifecycleItem? Selected { get; private set; }

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (DepartmentId == Guid.Empty)
        {
            return NotFound();
        }

        try
        {
            await _updateCode.ExecuteAsync(DepartmentId, DepartmentCode, cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = "Department code was updated.";
            return RedirectToPage("./Edit", new { departmentId = DepartmentId });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        return Page();
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        if (DepartmentId == Guid.Empty)
        {
            return false;
        }

        Selected = (await _lifecycle.ListDepartmentsAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => item.DepartmentId == DepartmentId);
        if (Selected is null)
        {
            return false;
        }

        if (!Selected.Capabilities.CanEdit)
        {
            return false;
        }

        DepartmentId = Selected.DepartmentId;
        if (string.IsNullOrWhiteSpace(ErrorMessage) && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            DepartmentCode = Selected.DepartmentCode;
        }

        return true;
    }
}
