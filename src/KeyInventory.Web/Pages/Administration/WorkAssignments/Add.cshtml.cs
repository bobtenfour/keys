using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkAssignments;

public sealed class AddModel : PageModel
{
    private const string SelectedMemberTempDataKey = "WorkAssignmentSelectedMemberCode";
    private const string SelectedMemberDisplayTempDataKey = "WorkAssignmentSelectedMemberDisplay";
    private const string SelectedRoomTempDataKey = "WorkAssignmentSelectedRoomCode";
    private const string SelectedRoomDisplayTempDataKey = "WorkAssignmentSelectedRoomDisplay";

    private readonly ICreateWorkAssignmentUseCase _create;
    private readonly ISearchActiveWorkforceMembersUseCase _searchMembers;
    private readonly ISearchActiveRoomsUseCase _searchRooms;

    public AddModel(
        ICreateWorkAssignmentUseCase create,
        ISearchActiveWorkforceMembersUseCase searchMembers,
        ISearchActiveRoomsUseCase searchRooms)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _searchMembers = searchMembers ?? throw new ArgumentNullException(nameof(searchMembers));
        _searchRooms = searchRooms ?? throw new ArgumentNullException(nameof(searchRooms));
    }

    [BindProperty]
    public string WorkAssignmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    [BindProperty]
    public bool IsPrimary { get; set; }

    [BindProperty]
    public string MemberSearchText { get; set; } = string.Empty;

    [BindProperty]
    public string RoomSearchText { get; set; } = string.Empty;

    public IReadOnlyList<EligibleKeyHolderCandidate> MemberMatches { get; private set; } = [];

    public IReadOnlyList<RoomListItem> RoomMatches { get; private set; } = [];

    public bool MemberSearchPerformed { get; private set; }

    public bool RoomSearchPerformed { get; private set; }

    public string? SelectedMemberDisplay { get; private set; }

    public string? SelectedRoomDisplay { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        RestoreSelectionsFromTempData();
    }

    public async Task<IActionResult> OnPostSearchMembersAsync(CancellationToken cancellationToken)
    {
        ClearSelectedMember();
        RestoreSelectedRoomFromTempData();
        MemberSearchPerformed = true;
        WorkforceMemberCode = string.Empty;
        SelectedMemberDisplay = null;
        MemberMatches = await _searchMembers
            .ExecuteAsync(MemberSearchText, ISearchActiveWorkforceMembersUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);
        return Page();
    }

    public IActionResult OnPostSelectMember(string workforceMemberCode, string? memberDisplay)
    {
        if (string.IsNullOrWhiteSpace(workforceMemberCode))
        {
            ErrorMessage = "Select a workforce member.";
            RestoreSelectedRoomFromTempData();
            return Page();
        }

        TempData[SelectedMemberTempDataKey] = workforceMemberCode.Trim();
        TempData[SelectedMemberDisplayTempDataKey] = string.IsNullOrWhiteSpace(memberDisplay)
            ? workforceMemberCode.Trim()
            : memberDisplay.Trim();
        return RedirectToPage();
    }

    public IActionResult OnPostClearMember()
    {
        ClearSelectedMember();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSearchRoomsAsync(CancellationToken cancellationToken)
    {
        ClearSelectedRoom();
        RestoreSelectedMemberFromTempData();
        RoomSearchPerformed = true;
        RoomCode = string.Empty;
        SelectedRoomDisplay = null;
        RoomMatches = await _searchRooms
            .ExecuteAsync(RoomSearchText, ISearchActiveRoomsUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);
        return Page();
    }

    public IActionResult OnPostSelectRoom(string roomCode, string? roomDisplay)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            ErrorMessage = "Select a room.";
            RestoreSelectedMemberFromTempData();
            return Page();
        }

        TempData[SelectedRoomTempDataKey] = roomCode.Trim();
        TempData[SelectedRoomDisplayTempDataKey] = string.IsNullOrWhiteSpace(roomDisplay)
            ? roomCode.Trim()
            : roomDisplay.Trim();
        return RedirectToPage();
    }

    public IActionResult OnPostClearRoom()
    {
        ClearSelectedRoom();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        RestoreSelectionsFromTempData();

        try
        {
            await _create.ExecuteAsync(
                    WorkAssignmentCode,
                    WorkforceMemberCode,
                    RoomCode,
                    IsPrimary,
                    cancellationToken)
                .ConfigureAwait(false);
            ClearSelectedMember();
            ClearSelectedRoom();
            TempData["SuccessMessage"] = "Work assignment was created.";
            return RedirectToPage("./Index");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            PersistCurrentSelections();
            return Page();
        }
    }

    private void RestoreSelectionsFromTempData()
    {
        RestoreSelectedMemberFromTempData();
        RestoreSelectedRoomFromTempData();
    }

    private void RestoreSelectedMemberFromTempData()
    {
        if (TempData.Peek(SelectedMemberTempDataKey) is string selectedCode
            && !string.IsNullOrWhiteSpace(selectedCode))
        {
            WorkforceMemberCode = selectedCode;
            SelectedMemberDisplay = TempData.Peek(SelectedMemberDisplayTempDataKey) as string ?? selectedCode;
        }
    }

    private void RestoreSelectedRoomFromTempData()
    {
        if (TempData.Peek(SelectedRoomTempDataKey) is string selectedCode
            && !string.IsNullOrWhiteSpace(selectedCode))
        {
            RoomCode = selectedCode;
            SelectedRoomDisplay = TempData.Peek(SelectedRoomDisplayTempDataKey) as string ?? selectedCode;
        }
    }

    private void PersistCurrentSelections()
    {
        if (!string.IsNullOrWhiteSpace(WorkforceMemberCode))
        {
            TempData[SelectedMemberTempDataKey] = WorkforceMemberCode;
            TempData[SelectedMemberDisplayTempDataKey] = SelectedMemberDisplay ?? WorkforceMemberCode;
        }

        if (!string.IsNullOrWhiteSpace(RoomCode))
        {
            TempData[SelectedRoomTempDataKey] = RoomCode;
            TempData[SelectedRoomDisplayTempDataKey] = SelectedRoomDisplay ?? RoomCode;
        }
    }

    private void ClearSelectedMember()
    {
        TempData.Remove(SelectedMemberTempDataKey);
        TempData.Remove(SelectedMemberDisplayTempDataKey);
    }

    private void ClearSelectedRoom()
    {
        TempData.Remove(SelectedRoomTempDataKey);
        TempData.Remove(SelectedRoomDisplayTempDataKey);
    }
}
