using KeyInventory.Application.Lifecycle;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkAssignments;

public sealed class IndexModel : PageModel
{
    private readonly IConfigurationLifecycleUseCase _lifecycle;
    private readonly IListWorkforceMembersUseCase _members;
    private readonly IListRoomsUseCase _rooms;
    private readonly IEndWorkAssignmentUseCase _end;

    public IndexModel(
        IConfigurationLifecycleUseCase lifecycle,
        IListWorkforceMembersUseCase members,
        IListRoomsUseCase rooms,
        IEndWorkAssignmentUseCase end)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _members = members ?? throw new ArgumentNullException(nameof(members));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _end = end ?? throw new ArgumentNullException(nameof(end));
    }

    public IReadOnlyList<WorkAssignmentLifecycleItem> Assignments { get; private set; } = [];

    public IReadOnlyDictionary<string, string> MemberDisplayByCode { get; private set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> RoomDisplayByCode { get; private set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostEndAsync(Guid workAssignmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _end.ExecuteAsync(workAssignmentId, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Room assignment was ended.";
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
        Assignments = await _lifecycle.ListWorkAssignmentsAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WorkforceMemberListItem> members = await _members.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        MemberDisplayByCode = members.ToDictionary(
            item => item.WorkforceMemberCode,
            item => PartyHolderDisplayFormatter.Format(item.FirstName, item.LastName, item.Uin),
            StringComparer.OrdinalIgnoreCase);
        RoomDisplayByCode = (await _rooms.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(
                item => item.RoomCode,
                item => RoomDisplayFormatter.Format(item),
                StringComparer.OrdinalIgnoreCase);
    }

    public string FormatMember(string workforceMemberCode)
    {
        return MemberDisplayByCode.TryGetValue(workforceMemberCode, out string? display)
            ? display
            : workforceMemberCode;
    }

    public string FormatRoom(string roomCode)
    {
        return RoomDisplayByCode.TryGetValue(roomCode, out string? display)
            ? display
            : roomCode;
    }
}
