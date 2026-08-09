using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class IndexModel : PageModel
{
    private readonly ICreateRoomUseCase _create;
    private readonly IListRoomsUseCase _list;
    private readonly IListBuildingsUseCase _buildings;
    private readonly IActivateRoomUseCase _activate;
    private readonly IRetireRoomUseCase _retire;

    public IndexModel(
        ICreateRoomUseCase create,
        IListRoomsUseCase list,
        IListBuildingsUseCase buildings,
        IActivateRoomUseCase activate,
        IRetireRoomUseCase retire)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
    }

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    [BindProperty]
    public string BuildingCode { get; set; } = string.Empty;

    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<RoomListItem> Rooms { get; private set; } = [];

    public IReadOnlyList<SelectListItem> BuildingOptions { get; private set; } = [];

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
            await _create.ExecuteAsync(RoomCode, BuildingCode, RoomNumber, Description, cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage = $"Room {RoomNumber} was created.";
            RoomCode = string.Empty;
            RoomNumber = string.Empty;
            Description = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostActivateAsync(string roomCode, CancellationToken cancellationToken)
    {
        try
        {
            await _activate.ExecuteAsync(roomCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Room {roomCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string roomCode, CancellationToken cancellationToken)
    {
        try
        {
            await _retire.ExecuteAsync(roomCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Room {roomCode} was retired.";
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
        Rooms = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        BuildingOptions = (await _buildings.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(item.BuildingCode, item.BuildingCode))
            .ToArray();
    }
}
