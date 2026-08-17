using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkAssignments;

public sealed class AddModel : PageModel
{
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
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public string? SuccessMessage { get; private set; }

    public void OnGet()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        WorkforceMemberCode = string.Empty;
        RoomCode = string.Empty;
    }

    /// <summary>
    /// JSON handler used by the member searchable combobox.
    /// </summary>
    public async Task<IActionResult> OnGetSearchMembersAsync(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<EligibleKeyHolderCandidate> matches = await _searchMembers
            .ExecuteAsync(q ?? string.Empty, ISearchActiveWorkforceMembersUseCase.DefaultMaxResults, cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(item => new
            {
                workforceMemberCode = item.WorkforceMemberCode,
                display = PartyHolderDisplayFormatter.Format(item.FirstName, item.LastName, item.Uin),
                firstName = item.FirstName,
                lastName = item.LastName,
                uin = item.Uin,
                departmentCode = item.DepartmentCode
            })
            .ToArray();
        return new JsonResult(result);
    }

    /// <summary>
    /// JSON handler used by the room searchable combobox.
    /// Returns no candidates until a workforce member is selected so Room options
    /// cannot cross Department boundaries.
    /// </summary>
    public async Task<IActionResult> OnGetSearchRoomsAsync(
        string? q,
        string? workforceMemberCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workforceMemberCode))
        {
            return new JsonResult(Array.Empty<object>());
        }

        IReadOnlyList<RoomListItem> matches = await _searchRooms
            .ExecuteForWorkforceMemberAsync(
                workforceMemberCode,
                q ?? string.Empty,
                ISearchActiveRoomsUseCase.DefaultMaxResults,
                cancellationToken)
            .ConfigureAwait(false);

        object[] result = matches
            .Select(item => new
            {
                roomCode = item.RoomCode,
                roomNumber = item.RoomNumber,
                description = item.Description ?? string.Empty,
                departmentCode = item.DepartmentCode
            })
            .ToArray();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(WorkforceMemberCode))
            {
                throw new InvalidOperationException("Select a workforce member.");
            }

            if (string.IsNullOrWhiteSpace(RoomCode))
            {
                throw new InvalidOperationException("Select a room in the workforce member's Department.");
            }

            await _create.ExecuteAsync(
                    WorkforceMemberCode,
                    RoomCode,
                    cancellationToken)
                .ConfigureAwait(false);

            TempData["SuccessMessage"] = "Room was assigned.";
            return RedirectToPage("./Add");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }
}
