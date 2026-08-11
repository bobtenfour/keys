using System.Text.Json;
using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Readiness;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Operations;

public sealed class IssueModel : PageModel
{
    private static readonly JsonSerializerOptions JustificationJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IIssueLoanUseCase _issueLoan;
    private readonly IListKeyAssetsUseCase _listKeyAssets;
    private readonly IListOpenLoansUseCase _listOpenLoans;
    private readonly IOperationalKeyLookupUseCase _lookup;
    private readonly IListWorkforceMembersUseCase _listMembers;
    private readonly IListWorkAssignmentsUseCase _listAssignments;
    private readonly IListRoomsUseCase _listRooms;
    private readonly IOperationalReadinessUseCase _readiness;

    public IssueModel(
        IIssueLoanUseCase issueLoan,
        IListKeyAssetsUseCase listKeyAssets,
        IListOpenLoansUseCase listOpenLoans,
        IOperationalKeyLookupUseCase lookup,
        IListWorkforceMembersUseCase listMembers,
        IListWorkAssignmentsUseCase listAssignments,
        IListRoomsUseCase listRooms,
        IOperationalReadinessUseCase readiness)
    {
        _issueLoan = issueLoan ?? throw new ArgumentNullException(nameof(issueLoan));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
        _listAssignments = listAssignments ?? throw new ArgumentNullException(nameof(listAssignments));
        _listRooms = listRooms ?? throw new ArgumentNullException(nameof(listRooms));
        _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    [BindProperty]
    public string LoanCode { get; set; } = string.Empty;

    [BindProperty]
    public string CatalogKeyCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string JustificationKind { get; set; } = "Department";

    [BindProperty]
    public string JustificationCode { get; set; } = string.Empty;

    [BindProperty]
    public string IssuedLocalText { get; set; } = string.Empty;

    [BindProperty]
    public string DueLocalText { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> KeyOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> WorkforceMemberOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> RoomOptions { get; private set; } = [];

    public string JustificationDataJson { get; private set; } = "{}";

    public OperationalReadinessViewModel Readiness { get; private set; } = null!;

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? catalogKeyCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(catalogKeyCode))
        {
            CatalogKeyCode = catalogKeyCode;
        }

        await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);
        IssuedLocalText = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow);
        DueLocalText = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow.AddDays(1));
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!OperatorLocalTimestamp.TryParseToUtc(IssuedLocalText, out DateTimeOffset issuedAtUtc, out string? issuedError))
            {
                throw new InvalidOperationException(issuedError ?? "Issued time is invalid.");
            }

            if (!OperatorLocalTimestamp.TryParseToUtc(DueLocalText, out DateTimeOffset dueAtUtc, out string? dueError))
            {
                throw new InvalidOperationException(dueError ?? "Due time is invalid.");
            }

            await _issueLoan.ExecuteAsync(
                    LoanCode,
                    CatalogKeyCode,
                    WorkforceMemberCode,
                    JustificationKind,
                    JustificationCode,
                    issuedAtUtc,
                    dueAtUtc,
                    cancellationToken)
                .ConfigureAwait(false);

            SuccessMessage = $"Key {CatalogKeyCode} was issued.";
            LoanCode = string.Empty;
            CatalogKeyCode = string.Empty;
            WorkforceMemberCode = string.Empty;
            JustificationCode = string.Empty;
            IssuedLocalText = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow);
            DueLocalText = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow.AddDays(1));
            ModelState.Clear();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        OperationalReadinessSnapshot snapshot = await _readiness.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Readiness = new OperationalReadinessViewModel(snapshot);

        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> issued = openItems.Select(item => item.CatalogKeyCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        KeyOptions = keys
            .Where(key => key.IsActive && !issued.Contains(key.CatalogKeyCode))
            .Select(key =>
            {
                string rooms = KeyOpenedRoomDisplayFormatter.Format(key.OpenedRooms);
                string text = string.IsNullOrEmpty(rooms)
                    ? $"{key.CatalogKeyCode} ({key.TypeCode})"
                    : $"{key.CatalogKeyCode} ({key.TypeCode}) — {rooms}";
                return new SelectListItem(
                    text,
                    key.CatalogKeyCode,
                    string.Equals(key.CatalogKeyCode, CatalogKeyCode, StringComparison.OrdinalIgnoreCase));
            })
            .ToArray();

        IReadOnlyList<WorkforceMemberIdentityDisplay> members = await _lookup
            .ListActiveWorkforceMembersWithIdentityAsync(cancellationToken)
            .ConfigureAwait(false);
        WorkforceMemberOptions = members
            .Select(member => new SelectListItem(
                PartyHolderDisplayFormatter.Format(member.FirstName, member.LastName, member.Uin),
                member.WorkforceMemberCode,
                string.Equals(member.WorkforceMemberCode, WorkforceMemberCode, StringComparison.Ordinal)))
            .ToArray();

        IReadOnlyList<WorkforceMemberListItem> memberRows = await _listMembers.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<WorkAssignmentListItem> assignments = await _listAssignments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RoomListItem> rooms = await _listRooms.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, RoomListItem> roomsByCode = rooms
            .Where(room => room.IsActive)
            .ToDictionary(room => room.RoomCode, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, MemberJustificationOptions> byMember = new(StringComparer.OrdinalIgnoreCase);
        foreach (WorkforceMemberListItem member in memberRows.Where(item =>
                     string.Equals(item.Status, "Active", StringComparison.Ordinal)))
        {
            List<JustificationChoice> departmentChoices =
            [
                new JustificationChoice(member.DepartmentCode, member.DepartmentCode)
            ];

            List<JustificationChoice> roomChoices = assignments
                .Where(item =>
                    item.IsActive
                    && string.Equals(item.WorkforceMemberCode, member.WorkforceMemberCode, StringComparison.Ordinal))
                .Select(item =>
                {
                    if (!roomsByCode.TryGetValue(item.RoomCode, out RoomListItem? room))
                    {
                        return new JustificationChoice(item.RoomCode, item.RoomCode);
                    }

                    return new JustificationChoice(item.RoomCode, RoomDisplayFormatter.Format(room));
                })
                .GroupBy(choice => choice.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(choice => choice.Label, StringComparer.Ordinal)
                .ToList();

            byMember[member.WorkforceMemberCode] = new MemberJustificationOptions(departmentChoices, roomChoices);
        }

        JustificationDataJson = JsonSerializer.Serialize(byMember, JustificationJsonOptions);

        if (!string.IsNullOrWhiteSpace(WorkforceMemberCode)
            && byMember.TryGetValue(WorkforceMemberCode, out MemberJustificationOptions? selected))
        {
            DepartmentOptions = selected.Departments
                .Select(choice => new SelectListItem(
                    choice.Label,
                    choice.Code,
                    string.Equals(choice.Code, JustificationCode, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            RoomOptions = selected.Rooms
                .Select(choice => new SelectListItem(
                    choice.Label,
                    choice.Code,
                    string.Equals(choice.Code, JustificationCode, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }
        else
        {
            DepartmentOptions = [];
            RoomOptions = [];
        }
    }

    private sealed record JustificationChoice(string Code, string Label);

    private sealed record MemberJustificationOptions(
        IReadOnlyList<JustificationChoice> Departments,
        IReadOnlyList<JustificationChoice> Rooms);
}
