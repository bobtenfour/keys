using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.Rooms;

public sealed class EditModel : PageModel
{
    private readonly IListRoomsUseCase _list;
    private readonly IUpdateRoomNumberUseCase _updateNumber;
    private readonly IUpdateRoomDescriptionUseCase _updateDescription;

    public EditModel(
        IListRoomsUseCase list,
        IUpdateRoomNumberUseCase updateNumber,
        IUpdateRoomDescriptionUseCase updateDescription)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _updateNumber = updateNumber ?? throw new ArgumentNullException(nameof(updateNumber));
        _updateDescription = updateDescription ?? throw new ArgumentNullException(nameof(updateDescription));
    }

    [BindProperty(SupportsGet = true)]
    public string? Room { get; set; }

    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public RoomListItem? Selected { get; private set; }

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
        if (string.IsNullOrWhiteSpace(Room))
        {
            return NotFound();
        }

        try
        {
            await _updateNumber.ExecuteAsync(Room, RoomNumber, cancellationToken).ConfigureAwait(false);
            await _updateDescription.ExecuteAsync(
                    Room,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = "Room was updated.";
            return RedirectToPage("./Edit", new { room = Room });
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
        if (string.IsNullOrWhiteSpace(Room))
        {
            return false;
        }

        IReadOnlyList<RoomListItem> rooms = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Selected = rooms.FirstOrDefault(item =>
            string.Equals(item.RoomCode, Room, StringComparison.OrdinalIgnoreCase));
        if (Selected is null)
        {
            return false;
        }

        Room = Selected.RoomCode;
        if (string.IsNullOrWhiteSpace(ErrorMessage) && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            RoomNumber = Selected.RoomNumber;
            Description = Selected.Description;
        }

        return true;
    }
}
