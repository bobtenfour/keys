using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class IndexModel : PageModel
{
    private readonly IListRoomsUseCase _list;
    private readonly IActivateRoomUseCase _activate;
    private readonly IRetireRoomUseCase _retire;

    public IndexModel(
        IListRoomsUseCase list,
        IActivateRoomUseCase activate,
        IRetireRoomUseCase retire)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _retire = retire ?? throw new ArgumentNullException(nameof(retire));
    }

    public IReadOnlyList<RoomListItem> Rooms { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Rooms = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
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

        Rooms = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
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

        Rooms = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
