using KeyInventory.Application.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class IndexModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;

    public IndexModel(IConfigurationLifecycleUseCase lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    public IReadOnlyList<RoomLifecycleItem> Rooms { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        Rooms = await _lifecycle.ListRoomsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostActivateAsync(string roomCode, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.ActivateRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Room {roomCode} was activated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Rooms = await _lifecycle.ListRoomsAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRetireAsync(string roomCode, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycle.RetireRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Room {roomCode} was retired.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        Rooms = await _lifecycle.ListRoomsAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }
}
