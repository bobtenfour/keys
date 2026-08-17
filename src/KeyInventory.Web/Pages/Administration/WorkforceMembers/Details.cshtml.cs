using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class DetailsModel : PageModel
{
    private readonly IListWorkforceMembersUseCase _listMembers;
    private readonly IListDepartmentsUseCase _listDepartments;
    private readonly IListWorkAssignmentsUseCase _listAssignments;
    private readonly IListRoomsUseCase _listRooms;
    private readonly IOperationalKeyLookupUseCase _lookup;
    private readonly IListOutstandingReturnObligationsUseCase _obligations;
    private readonly IUpdateWorkforceMemberDepartmentUseCase _updateDepartment;
    private readonly IUpdateWorkforceMemberWorkforceTypeUseCase _updateType;
    private readonly IUpdatePartyNameUseCase _updatePartyName;
    private readonly ICorrectPartyUinUseCase _correctPartyUin;
    private readonly ITerminateWorkforceMemberUseCase _terminate;

    public DetailsModel(
        IListWorkforceMembersUseCase listMembers,
        IListDepartmentsUseCase listDepartments,
        IListWorkAssignmentsUseCase listAssignments,
        IListRoomsUseCase listRooms,
        IOperationalKeyLookupUseCase lookup,
        IListOutstandingReturnObligationsUseCase obligations,
        IUpdateWorkforceMemberDepartmentUseCase updateDepartment,
        IUpdateWorkforceMemberWorkforceTypeUseCase updateType,
        IUpdatePartyNameUseCase updatePartyName,
        ICorrectPartyUinUseCase correctPartyUin,
        ITerminateWorkforceMemberUseCase terminate)
    {
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
        _listDepartments = listDepartments ?? throw new ArgumentNullException(nameof(listDepartments));
        _listAssignments = listAssignments ?? throw new ArgumentNullException(nameof(listAssignments));
        _listRooms = listRooms ?? throw new ArgumentNullException(nameof(listRooms));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _obligations = obligations ?? throw new ArgumentNullException(nameof(obligations));
        _updateDepartment = updateDepartment ?? throw new ArgumentNullException(nameof(updateDepartment));
        _updateType = updateType ?? throw new ArgumentNullException(nameof(updateType));
        _updatePartyName = updatePartyName ?? throw new ArgumentNullException(nameof(updatePartyName));
        _correctPartyUin = correctPartyUin ?? throw new ArgumentNullException(nameof(correctPartyUin));
        _terminate = terminate ?? throw new ArgumentNullException(nameof(terminate));
    }

    [BindProperty(SupportsGet = true)]
    public string? Member { get; set; }

    /// <summary>
    /// Explicit Edit intent. Default View shows membership as information.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Edit { get; set; }

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public string Uin { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceType { get; set; } = "Employee";

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmTerminate { get; set; }

    public WorkforceMemberListItem? Selected { get; private set; }

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<WorkforceMemberRoomAssignmentRow> RoomAssignments { get; private set; } = [];

    public IReadOnlyList<IssuedKeyForMemberItem> IssuedKeys { get; private set; } = [];

    public IReadOnlyList<OutstandingReturnObligationItem> Obligations { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool JustCreated { get; private set; }

    public bool IsEditMode => Edit;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        JustCreated = (TempData.ContainsKey("JustCreated") && TempData["JustCreated"] is true)
            || (SuccessMessage is not null
                && SuccessMessage.Contains("was created", StringComparison.OrdinalIgnoreCase));

        if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMaintainAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Member))
        {
            return NotFound();
        }

        try
        {
            WorkforceMemberListItem? selected = await FindSelectedAsync(cancellationToken).ConfigureAwait(false);
            if (selected is null)
            {
                return NotFound();
            }

            await _updateDepartment.ExecuteAsync(Member, DepartmentCode, cancellationToken).ConfigureAwait(false);
            await _updateType.ExecuteAsync(Member, WorkforceType, cancellationToken).ConfigureAwait(false);
            await _updatePartyName.ExecuteAsync(selected.PartyCode, FirstName, LastName, cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(Uin.Trim(), selected.Uin, StringComparison.Ordinal))
            {
                await _correctPartyUin.ExecuteAsync(selected.PartyCode, Uin, cancellationToken).ConfigureAwait(false);
            }

            TempData["SuccessMessage"] = "Workforce member was updated.";
            return RedirectToPage("./Details", new { member = Member });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            Edit = true;
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
            {
                return NotFound();
            }

            return Page();
        }
    }

    public async Task<IActionResult> OnPostTerminateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Member))
        {
            return NotFound();
        }

        if (!ConfirmTerminate)
        {
            ErrorMessage = "Confirm termination before continuing.";
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
            {
                return NotFound();
            }

            return Page();
        }

        try
        {
            await _terminate.ExecuteAsync(Member, cancellationToken).ConfigureAwait(false);
            TempData["SuccessMessage"] = "Workforce member was terminated.";
            return RedirectToPage("./Details", new { member = Member });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            if (!await LoadAsync(cancellationToken).ConfigureAwait(false))
            {
                return NotFound();
            }

            return Page();
        }
    }

    private async Task<WorkforceMemberListItem?> FindSelectedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkforceMemberListItem> members = await _listMembers.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        return members.FirstOrDefault(item =>
            string.Equals(item.WorkforceMemberCode, Member, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Member))
        {
            return false;
        }

        IReadOnlyList<WorkforceMemberListItem> members = await _listMembers.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        Selected = members.FirstOrDefault(item =>
            string.Equals(item.WorkforceMemberCode, Member, StringComparison.OrdinalIgnoreCase));
        if (Selected is null)
        {
            return false;
        }

        Member = Selected.WorkforceMemberCode;

        if (IsEditMode || !string.IsNullOrWhiteSpace(ErrorMessage))
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                FirstName = Selected.FirstName;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                LastName = Selected.LastName;
            }

            if (string.IsNullOrWhiteSpace(Uin))
            {
                Uin = Selected.Uin;
            }

            if (string.IsNullOrWhiteSpace(WorkforceType))
            {
                WorkforceType = Selected.WorkforceType;
            }

            if (string.IsNullOrWhiteSpace(DepartmentCode))
            {
                DepartmentCode = Selected.DepartmentCode;
            }
        }

        IReadOnlyList<DepartmentListItem> departments = await _listDepartments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        string selectedDepartment = string.IsNullOrWhiteSpace(DepartmentCode)
            ? Selected.DepartmentCode
            : DepartmentCode;
        DepartmentOptions = departments
            .Where(item =>
                item.IsActive
                || string.Equals(item.DepartmentCode, selectedDepartment, StringComparison.OrdinalIgnoreCase))
            .Select(item => new SelectListItem(
                item.DepartmentCode,
                item.DepartmentCode,
                string.Equals(item.DepartmentCode, selectedDepartment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        IReadOnlyList<WorkAssignmentListItem> assignments = await _listAssignments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RoomListItem> rooms = await _listRooms.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, RoomListItem> roomsByCode = rooms.ToDictionary(
            item => item.RoomCode,
            StringComparer.OrdinalIgnoreCase);

        RoomAssignments = assignments
            .Where(item => item.IsActive
                && string.Equals(item.WorkforceMemberCode, Selected.WorkforceMemberCode, StringComparison.Ordinal))
            .Select(item =>
            {
                roomsByCode.TryGetValue(item.RoomCode, out RoomListItem? room);
                string roomNumber = room?.RoomNumber ?? item.RoomCode;
                string description = room?.Description ?? string.Empty;
                return new WorkforceMemberRoomAssignmentRow(roomNumber, description);
            })
            .ToArray();

        IssuedKeys = await _lookup
            .ListIssuedKeysForWorkforceMemberAsync(Selected.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        // Outstanding return obligations are an Application capability for Terminated members only.
        if (string.Equals(Selected.Status, "Terminated", StringComparison.Ordinal))
        {
            Obligations = await _obligations.ExecuteAsync(Selected.WorkforceMemberCode, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            Obligations = [];
        }

        return true;
    }
}

public sealed record WorkforceMemberRoomAssignmentRow(
    string RoomNumber,
    string Description);
