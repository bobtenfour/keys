using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class DetailsModel : PageModel
{
    private readonly IListWorkforceMembersUseCase _listMembers;
    private readonly IListOrganizationsUseCase _listOrganizations;
    private readonly IListDepartmentsUseCase _listDepartments;
    private readonly IListWorkAssignmentsUseCase _listAssignments;
    private readonly IListRoomsUseCase _listRooms;
    private readonly IListBuildingsUseCase _listBuildings;
    private readonly IOperationalKeyLookupUseCase _lookup;
    private readonly IListOutstandingReturnObligationsUseCase _obligations;
    private readonly IUpdateWorkforceMemberOrganizationDepartmentUseCase _updateOrgDept;
    private readonly IUpdateWorkforceMemberResponsibleManagerUseCase _updateManager;
    private readonly IUpdateWorkforceMemberWorkforceTypeUseCase _updateType;
    private readonly ITerminateWorkforceMemberUseCase _terminate;

    public DetailsModel(
        IListWorkforceMembersUseCase listMembers,
        IListOrganizationsUseCase listOrganizations,
        IListDepartmentsUseCase listDepartments,
        IListWorkAssignmentsUseCase listAssignments,
        IListRoomsUseCase listRooms,
        IListBuildingsUseCase listBuildings,
        IOperationalKeyLookupUseCase lookup,
        IListOutstandingReturnObligationsUseCase obligations,
        IUpdateWorkforceMemberOrganizationDepartmentUseCase updateOrgDept,
        IUpdateWorkforceMemberResponsibleManagerUseCase updateManager,
        IUpdateWorkforceMemberWorkforceTypeUseCase updateType,
        ITerminateWorkforceMemberUseCase terminate)
    {
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
        _listOrganizations = listOrganizations ?? throw new ArgumentNullException(nameof(listOrganizations));
        _listDepartments = listDepartments ?? throw new ArgumentNullException(nameof(listDepartments));
        _listAssignments = listAssignments ?? throw new ArgumentNullException(nameof(listAssignments));
        _listRooms = listRooms ?? throw new ArgumentNullException(nameof(listRooms));
        _listBuildings = listBuildings ?? throw new ArgumentNullException(nameof(listBuildings));
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _obligations = obligations ?? throw new ArgumentNullException(nameof(obligations));
        _updateOrgDept = updateOrgDept ?? throw new ArgumentNullException(nameof(updateOrgDept));
        _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
        _updateType = updateType ?? throw new ArgumentNullException(nameof(updateType));
        _terminate = terminate ?? throw new ArgumentNullException(nameof(terminate));
    }

    [BindProperty(SupportsGet = true)]
    public string? Member { get; set; }

    [BindProperty]
    public string WorkforceType { get; set; } = "Employee";

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string ResponsibleManagerWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public bool ConfirmTerminate { get; set; }

    public WorkforceMemberListItem? Selected { get; private set; }

    public string ManagerDisplay { get; private set; } = string.Empty;

    public IReadOnlyList<SelectListItem> OrganizationOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> ManagerOptions { get; private set; } = [];

    public IReadOnlyList<WorkforceMemberWorkAssignmentRow> WorkAssignments { get; private set; } = [];

    public IReadOnlyList<IssuedKeyForMemberItem> IssuedKeys { get; private set; } = [];

    public IReadOnlyList<OutstandingReturnObligationItem> Obligations { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
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
            await _updateOrgDept.ExecuteAsync(Member, OrganizationCode, DepartmentCode, cancellationToken)
                .ConfigureAwait(false);
            await _updateManager.ExecuteAsync(Member, ResponsibleManagerWorkforceMemberCode, cancellationToken)
                .ConfigureAwait(false);
            await _updateType.ExecuteAsync(Member, WorkforceType, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Workforce member was updated.";
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
            Obligations = await _obligations.ExecuteAsync(Member, cancellationToken).ConfigureAwait(false);
            SuccessMessage = "Workforce member was terminated.";
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
        if (string.IsNullOrWhiteSpace(ErrorMessage) && string.IsNullOrWhiteSpace(SuccessMessage))
        {
            WorkforceType = Selected.WorkforceType;
            OrganizationCode = Selected.OrganizationCode;
            DepartmentCode = Selected.DepartmentCode;
            ResponsibleManagerWorkforceMemberCode = Selected.ResponsibleManagerWorkforceMemberCode;
        }

        WorkforceMemberListItem? manager = members.FirstOrDefault(item =>
            string.Equals(
                item.WorkforceMemberCode,
                Selected.ResponsibleManagerWorkforceMemberCode,
                StringComparison.Ordinal));
        ManagerDisplay = manager is null
            ? Selected.ResponsibleManagerWorkforceMemberCode
            : PartyHolderDisplayFormatter.Format(manager.FirstName, manager.LastName, manager.Uin);

        IReadOnlyList<OrganizationListItem> organizations = await _listOrganizations.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        OrganizationOptions = organizations
            .Where(item => item.IsActive
                || string.Equals(item.OrganizationCode, OrganizationCode, StringComparison.OrdinalIgnoreCase))
            .Select(item => new SelectListItem(
                item.OrganizationCode,
                item.OrganizationCode,
                string.Equals(item.OrganizationCode, OrganizationCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        IReadOnlyList<DepartmentListItem> departments = await _listDepartments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        DepartmentOptions = departments
            .Where(item =>
                (item.IsActive
                    || string.Equals(item.DepartmentCode, DepartmentCode, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(OrganizationCode)
                    || string.Equals(item.OrganizationCode, OrganizationCode, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new SelectListItem(
                $"{item.OrganizationCode} / {item.DepartmentCode}",
                item.DepartmentCode,
                string.Equals(item.DepartmentCode, DepartmentCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        ManagerOptions = members
            .Where(item => string.Equals(item.Status, "Active", StringComparison.Ordinal)
                && !string.Equals(item.WorkforceMemberCode, Selected.WorkforceMemberCode, StringComparison.Ordinal))
            .Select(item => new SelectListItem(
                PartyHolderDisplayFormatter.Format(item.FirstName, item.LastName, item.Uin),
                item.WorkforceMemberCode,
                string.Equals(
                    item.WorkforceMemberCode,
                    ResponsibleManagerWorkforceMemberCode,
                    StringComparison.Ordinal)))
            .ToArray();

        IReadOnlyList<WorkAssignmentListItem> assignments = await _listAssignments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RoomListItem> rooms = await _listRooms.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<BuildingListItem> buildings = await _listBuildings.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, RoomListItem> roomsByCode = rooms.ToDictionary(
            item => item.RoomCode,
            StringComparer.OrdinalIgnoreCase);
        HashSet<string> activeBuildings = buildings
            .Select(item => item.BuildingCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        WorkAssignments = assignments
            .Where(item => item.IsActive
                && string.Equals(item.WorkforceMemberCode, Selected.WorkforceMemberCode, StringComparison.Ordinal))
            .Select(item =>
            {
                roomsByCode.TryGetValue(item.RoomCode, out RoomListItem? room);
                string building = room?.BuildingCode ?? string.Empty;
                string roomNumber = room?.RoomNumber ?? item.RoomCode;
                string description = room?.Description ?? string.Empty;
                return new WorkforceMemberWorkAssignmentRow(
                    item.WorkAssignmentCode,
                    building,
                    roomNumber,
                    description,
                    item.IsPrimary,
                    !string.IsNullOrEmpty(building) && activeBuildings.Contains(building));
            })
            .ToArray();

        IssuedKeys = await _lookup
            .ListIssuedKeysForWorkforceMemberAsync(Selected.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}

public sealed record WorkforceMemberWorkAssignmentRow(
    string WorkAssignmentCode,
    string BuildingCode,
    string RoomNumber,
    string Description,
    bool IsPrimary,
    bool BuildingKnown);
