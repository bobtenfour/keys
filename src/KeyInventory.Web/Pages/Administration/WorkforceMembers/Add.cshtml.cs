using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class AddModel : PageModel
{
    private readonly IRegisterWorkforceMemberUseCase _register;
    private readonly IListDepartmentsUseCase _listDepartments;

    public AddModel(
        IRegisterWorkforceMemberUseCase register,
        IListDepartmentsUseCase listDepartments)
    {
        _register = register ?? throw new ArgumentNullException(nameof(register));
        _listDepartments = listDepartments ?? throw new ArgumentNullException(nameof(listDepartments));
    }

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public string Uin { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceType { get; set; } = "Employee";

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            string memberCode = await _register.ExecuteAsync(
                    FirstName,
                    LastName,
                    Uin,
                    WorkforceType,
                    DepartmentCode,
                    cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Workforce member {FirstName.Trim()} {LastName.Trim()} was created.";
            TempData["JustCreated"] = true;
            return RedirectToPage("./Details", new { member = memberCode });
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
        IReadOnlyList<DepartmentListItem> departments = await _listDepartments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        DepartmentOptions = departments
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(
                item.DepartmentCode,
                item.DepartmentCode,
                string.Equals(item.DepartmentCode, DepartmentCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}
