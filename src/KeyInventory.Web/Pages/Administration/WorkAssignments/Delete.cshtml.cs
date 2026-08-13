using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkAssignments;

public sealed class DeleteModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;
    private readonly IListWorkforceMembersUseCase _members;
    private readonly IListRoomsUseCase _rooms;

    public DeleteModel(
        IConfigurationLifecycleUseCase lifecycle,
        IListWorkforceMembersUseCase members,
        IListRoomsUseCase rooms)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _members = members ?? throw new ArgumentNullException(nameof(members));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
    }

    [BindProperty(SupportsGet = true)]
    public string WorkAssignmentCode { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmDelete { get; set; }

    public WorkAssignmentLifecycleItem? Item { get; private set; }

    public string MemberDisplay { get; private set; } = string.Empty;

    public string RoomDisplay { get; private set; } = string.Empty;

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
                ?? "This work assignment can no longer be deleted. End it instead to preserve history.";
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
            await _lifecycle.DeleteWorkAssignmentAsync(WorkAssignmentCode, cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = $"Work assignment \"{WorkAssignmentCode}\" was deleted.";
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
        if (string.IsNullOrWhiteSpace(WorkAssignmentCode))
        {
            return false;
        }

        string code = WorkAssignmentCode.Trim();
        Item = (await _lifecycle.ListWorkAssignmentsAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item =>
                string.Equals(item.WorkAssignmentCode, code, StringComparison.OrdinalIgnoreCase));
        if (Item is null)
        {
            return false;
        }

        WorkAssignmentCode = Item.WorkAssignmentCode;

        WorkforceMemberListItem? member = (await _members.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item =>
                string.Equals(
                    item.WorkforceMemberCode,
                    Item.WorkforceMemberCode,
                    StringComparison.OrdinalIgnoreCase));
        MemberDisplay = member is null
            ? Item.WorkforceMemberCode
            : PartyHolderDisplayFormatter.Format(member.FirstName, member.LastName, member.Uin);

        RoomListItem? room = (await _rooms.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item =>
                string.Equals(item.RoomCode, Item.RoomCode, StringComparison.OrdinalIgnoreCase));
        RoomDisplay = room is null ? Item.RoomCode : RoomDisplayFormatter.Format(room);
        return true;
    }

    private static string FormatDeleteError(InvalidOperationException exception)
    {
        if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return exception.Message;
        }

        return "This work assignment can no longer be deleted. End it instead to preserve history.";
    }
}
