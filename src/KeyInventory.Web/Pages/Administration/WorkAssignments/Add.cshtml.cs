using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkAssignments;

public sealed class AddModel : PageModel
{
    private readonly ICreateWorkAssignmentUseCase _create;
    private readonly IListWorkforceMembersUseCase _members;
    private readonly IListRoomsUseCase _rooms;

    public AddModel(
        ICreateWorkAssignmentUseCase create,
        IListWorkforceMembersUseCase members,
        IListRoomsUseCase rooms)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _members = members ?? throw new ArgumentNullException(nameof(members));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
    }

    [BindProperty]
    public string WorkAssignmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    [BindProperty]
    public bool IsPrimary { get; set; }

    public IReadOnlyList<SelectListItem> MemberOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _create.ExecuteAsync(
                    WorkAssignmentCode,
                    WorkforceMemberCode,
                    RoomCode,
                    IsPrimary,
                    cancellationToken)
                .ConfigureAwait(false);
            TempData["SuccessMessage"] = "Work assignment was created.";
            return RedirectToPage("./Index");
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
        MemberOptions = (await _members.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => string.Equals(item.Status, "Active", StringComparison.Ordinal))
            .Select(item => new SelectListItem(
                PartyHolderDisplayFormatter.Format(item.FirstName, item.LastName, item.Uin),
                item.WorkforceMemberCode,
                string.Equals(item.WorkforceMemberCode, WorkforceMemberCode, StringComparison.Ordinal)))
            .ToArray();
        RoomOptions = (await _rooms.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(
                RoomDisplayFormatter.Format(item),
                item.RoomCode,
                string.Equals(item.RoomCode, RoomCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}
