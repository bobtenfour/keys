using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class AddModel : PageModel
{
    private readonly ICreateRoomUseCase _create;
    private readonly IListDepartmentsUseCase _listDepartments;

    public AddModel(ICreateRoomUseCase create, IListDepartmentsUseCase listDepartments)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _listDepartments = listDepartments ?? throw new ArgumentNullException(nameof(listDepartments));
    }

    [BindProperty]
    public Guid DepartmentId { get; set; }

    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDepartmentOptionsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (DepartmentId == Guid.Empty)
            {
                throw new InvalidOperationException("Select the Department this Room belongs to.");
            }

            await _create.ExecuteAsync(
                    DepartmentId,
                    RoomNumber,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Room {RoomNumber.Trim()} was created.";
            return RedirectToPage("./Index");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            await LoadDepartmentOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }
    }

    private async Task LoadDepartmentOptionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentListItem> departments = await _listDepartments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        DepartmentOptions = departments
            .Where(dept => dept.IsActive)
            .OrderBy(dept => dept.DepartmentCode, StringComparer.OrdinalIgnoreCase)
            .Select(dept => new SelectListItem(
                dept.DepartmentCode,
                dept.DepartmentId.ToString("D"),
                dept.DepartmentId == DepartmentId))
            .ToArray();
    }
}
