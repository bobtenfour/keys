using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Departments;

public sealed class AddModel : PageModel
{
    private readonly ICreateDepartmentUseCase _create;

    public AddModel(ICreateDepartmentUseCase create)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(DepartmentCode, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Department {DepartmentCode.Trim()} was created.";
            return RedirectToPage("./Index");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }
}
