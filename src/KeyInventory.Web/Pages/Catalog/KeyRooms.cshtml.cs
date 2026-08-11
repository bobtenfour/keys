using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Catalog;

public sealed class KeyRoomsModel : PageModel
{
    private readonly IKeyRoomAssignmentUseCase _assignments;
    private readonly IListKeyAssetsUseCase _listKeys;
    private readonly IListRoomsUseCase _listRooms;

    public KeyRoomsModel(
        IKeyRoomAssignmentUseCase assignments,
        IListKeyAssetsUseCase listKeys,
        IListRoomsUseCase listRooms)
    {
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _listKeys = listKeys ?? throw new ArgumentNullException(nameof(listKeys));
        _listRooms = listRooms ?? throw new ArgumentNullException(nameof(listRooms));
    }

    [BindProperty(SupportsGet = true)]
    public string Key { get; set; } = string.Empty;

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    public IReadOnlyList<KeyOpenedRoomItem> OpenedRooms { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return RedirectToPage("/Catalog/Keys");
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _assignments.AssignRoomAsync(Key, RoomCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Room assignment saved.";
            RoomCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string roomCode, CancellationToken cancellationToken)
    {
        try
        {
            await _assignments.RemoveRoomAsync(Key, roomCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Room assignment removed.";
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
        IReadOnlyList<KeyAssetListItem> keys = await _listKeys.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (!keys.Any(item => string.Equals(item.CatalogKeyCode, Key, StringComparison.Ordinal)))
        {
            ErrorMessage ??= "The key was not found in the catalog.";
            OpenedRooms = [];
            RoomOptions = [];
            return;
        }

        OpenedRooms = await _assignments.ListOpenedRoomsAsync(Key, cancellationToken).ConfigureAwait(false);
        HashSet<string> assigned = OpenedRooms.Select(room => room.RoomCode).ToHashSet(StringComparer.Ordinal);
        RoomOptions = (await _listRooms.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(room => room.IsActive && !assigned.Contains(room.RoomCode))
            .OrderBy(room => room.RoomNumber, StringComparer.Ordinal)
            .Select(room => new SelectListItem(
                $"{RoomDisplayFormatter.Format(room)} ({room.RoomCode})",
                room.RoomCode))
            .ToArray();
    }
}
