using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class DeleteModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public DeleteModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    [BindProperty(SupportsGet = true)]
    public Guid DepartmentId { get; set; }

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public DepartmentLifecycleItem? Item { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!Item!.Capabilities.CanDelete)
        {
            TempData["ErrorMessage"] = Item.Capabilities.DeleteBlockedReason
                ?? "This department can no longer be deleted because it is in use. Retire it instead to preserve its history.";
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        if (!ConfirmDelete)
        {
            ErrorMessage = "Confirm deletion before continuing.";
            return Page();
        }

        try
        {
            await _lifecycle.DeleteDepartmentAsync(DepartmentId, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Department \"{Item!.DepartmentCode}\" was deleted.";
            return RedirectToPage("./Index");
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = FormatDeleteError(exception);
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false)
                || Item is null
                || !Item.Capabilities.CanDelete)
            {
                TempData["ErrorMessage"] = ErrorMessage;
                return RedirectToPage("./Index");
            }

            return Page();
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        if (DepartmentId == Guid.Empty)
        {
            return false;
        }

        Item = (await _lifecycle.ListDepartmentsAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => item.DepartmentId == DepartmentId);

        return Item is not null;
    }

    private static string FormatDeleteError(InvalidOperationException exception)
    {
        if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return exception.Message;
        }

        return "This department can no longer be deleted because it is in use. Retire it instead to preserve its history.";
    }
}
