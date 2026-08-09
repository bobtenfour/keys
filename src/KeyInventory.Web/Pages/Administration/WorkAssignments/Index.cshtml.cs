using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkAssignments;

public sealed class IndexModel : PageModel
{
    private readonly ICreateWorkAssignmentUseCase _create;
    private readonly IListWorkAssignmentsUseCase _list;
    private readonly IListWorkforceMembersUseCase _members;
    private readonly IListRoomsUseCase _rooms;
    private readonly IEndWorkAssignmentUseCase _end;
    private readonly IMarkWorkAssignmentPrimaryUseCase _markPrimary;
    private readonly IClearWorkAssignmentPrimaryUseCase _clearPrimary;

    public IndexModel(
        ICreateWorkAssignmentUseCase create,
        IListWorkAssignmentsUseCase list,
        IListWorkforceMembersUseCase members,
        IListRoomsUseCase rooms,
        IEndWorkAssignmentUseCase end,
        IMarkWorkAssignmentPrimaryUseCase markPrimary,
        IClearWorkAssignmentPrimaryUseCase clearPrimary)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _members = members ?? throw new ArgumentNullException(nameof(members));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _end = end ?? throw new ArgumentNullException(nameof(end));
        _markPrimary = markPrimary ?? throw new ArgumentNullException(nameof(markPrimary));
        _clearPrimary = clearPrimary ?? throw new ArgumentNullException(nameof(clearPrimary));
    }

    [BindProperty]
    public string WorkAssignmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string RoomCode { get; set; } = string.Empty;

    [BindProperty]
    public bool IsPrimary { get; set; }

    public IReadOnlyList<WorkAssignmentListItem> Assignments { get; private set; } = [];

    public IReadOnlyList<SelectListItem> MemberOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

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
            SuccessMessage = $"Work assignment {WorkAssignmentCode} was created.";
            WorkAssignmentCode = string.Empty;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostEndAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        try
        {
            await _end.ExecuteAsync(workAssignmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Work assignment {workAssignmentCode} was ended.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkPrimaryAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        try
        {
            await _markPrimary.ExecuteAsync(workAssignmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Work assignment {workAssignmentCode} is now primary.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostClearPrimaryAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        try
        {
            await _clearPrimary.ExecuteAsync(workAssignmentCode, cancellationToken).ConfigureAwait(false);
            SuccessMessage = $"Primary cleared for work assignment {workAssignmentCode}.";
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
        Assignments = await _list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        MemberOptions = (await _members.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => string.Equals(item.Status, "Active", StringComparison.Ordinal))
            .Select(item => new SelectListItem(item.WorkforceMemberCode, item.WorkforceMemberCode))
            .ToArray();
        RoomOptions = (await _rooms.ExecuteAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem($"{item.BuildingCode} / {item.RoomNumber}", item.RoomCode))
            .ToArray();
    }
}
