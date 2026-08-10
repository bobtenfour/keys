using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class AddModel : PageModel
{
    private readonly ICreateRoomUseCase _create;
    private readonly IListBuildingsUseCase _buildings;

    public AddModel(ICreateRoomUseCase create, IListBuildingsUseCase buildings)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
    }

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    [BindProperty]
    public string BuildingCode { get; set; } = string.Empty;

    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> BuildingOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(RoomCode, BuildingCode, RoomNumber, Description, cancellationToken)
                .ConfigureAwait(false);
            return RedirectToPage("./Index");
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
        BuildingOptions = (await _buildings.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(
                item.BuildingCode,
                item.BuildingCode,
                string.Equals(item.BuildingCode, BuildingCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}
