using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class IndexModel : PageModel
{
    private readonly ICreatePartyUseCase _createParty;
    private readonly ICreateWorkforceMemberUseCase _createMember;
    private readonly ICreateBootstrapWorkforcePairUseCase _createBootstrapPair;
    private readonly IListWorkforceMembersUseCase _listMembers;
    private readonly ITerminateWorkforceMemberUseCase _terminate;
    private readonly IListOutstandingReturnObligationsUseCase _obligations;
    private readonly IUpdateWorkforceMemberOrganizationDepartmentUseCase _updateOrgDept;
    private readonly IUpdateWorkforceMemberResponsibleManagerUseCase _updateManager;
    private readonly IUpdateWorkforceMemberWorkforceTypeUseCase _updateType;

    public IndexModel(
        ICreatePartyUseCase createParty,
        ICreateWorkforceMemberUseCase createMember,
        ICreateBootstrapWorkforcePairUseCase createBootstrapPair,
        IListWorkforceMembersUseCase listMembers,
        ITerminateWorkforceMemberUseCase terminate,
        IListOutstandingReturnObligationsUseCase obligations,
        IUpdateWorkforceMemberOrganizationDepartmentUseCase updateOrgDept,
        IUpdateWorkforceMemberResponsibleManagerUseCase updateManager,
        IUpdateWorkforceMemberWorkforceTypeUseCase updateType)
    {
        _createParty = createParty ?? throw new ArgumentNullException(nameof(createParty));
        _createMember = createMember ?? throw new ArgumentNullException(nameof(createMember));
        _createBootstrapPair = createBootstrapPair ?? throw new ArgumentNullException(nameof(createBootstrapPair));
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
        _terminate = terminate ?? throw new ArgumentNullException(nameof(terminate));
        _obligations = obligations ?? throw new ArgumentNullException(nameof(obligations));
        _updateOrgDept = updateOrgDept ?? throw new ArgumentNullException(nameof(updateOrgDept));
        _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
        _updateType = updateType ?? throw new ArgumentNullException(nameof(updateType));
    }

    [BindProperty]
    public string PartyCode { get; set; } = string.Empty;

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public string Uin { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceType { get; set; } = "Employee";

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string ResponsibleManagerWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string PeerPartyCode { get; set; } = string.Empty;

    [BindProperty]
    public string PeerFirstName { get; set; } = string.Empty;

    [BindProperty]
    public string PeerLastName { get; set; } = string.Empty;

    [BindProperty]
    public string PeerUin { get; set; } = string.Empty;

    [BindProperty]
    public string PeerWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string PeerWorkforceType { get; set; } = "Employee";

    [BindProperty]
    public string TerminateWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string MaintainWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string MaintainOrganizationCode { get; set; } = string.Empty;

    [BindProperty]
    public string MaintainDepartmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string MaintainResponsibleManagerWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string MaintainWorkforceType { get; set; } = "Employee";

    public bool IsBootstrap { get; private set; }

    public IReadOnlyList<WorkforceMemberListItem> Members { get; private set; } = [];

    public IReadOnlyList<SelectListItem> ManagerOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> ActiveMemberOptions { get; private set; } = [];

    public IReadOnlyList<OutstandingReturnObligationItem> Obligations { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostCreatePartyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _createParty.ExecuteAsync(PartyCode, FirstName, LastName, Uin, cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage = $"Party {PartyCode} was created.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateMemberAsync(CancellationToken cancellationToken)
    {
        try
        {
            Members = await _listMembers.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (Members.Count == 0)
            {
                await _createParty.ExecuteAsync(PartyCode, FirstName, LastName, Uin, cancellationToken)
                    .ConfigureAwait(false);
                await _createParty.ExecuteAsync(PeerPartyCode, PeerFirstName, PeerLastName, PeerUin, cancellationToken)
                    .ConfigureAwait(false);
                await _createBootstrapPair.ExecuteAsync(
                        WorkforceMemberCode,
                        PartyCode,
                        WorkforceType,
                        PeerWorkforceMemberCode,
                        PeerPartyCode,
                        PeerWorkforceType,
                        OrganizationCode,
                        DepartmentCode,
                        cancellationToken)
                    .ConfigureAwait(false);
                SuccessMessage = "Initial workforce member pair was created.";
            }
            else
            {
                await _createMember.ExecuteAsync(
                        WorkforceMemberCode,
                        PartyCode,
                        WorkforceType,
                        OrganizationCode,
                        DepartmentCode,
                        ResponsibleManagerWorkforceMemberCode,
                        cancellationToken)
                    .ConfigureAwait(false);
                SuccessMessage = $"Workforce member {WorkforceMemberCode} was created.";
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostTerminateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _terminate.ExecuteAsync(TerminateWorkforceMemberCode, cancellationToken).ConfigureAwait(false);
            Obligations = await _obligations.ExecuteAsync(TerminateWorkforceMemberCode, cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage = $"Workforce member {TerminateWorkforceMemberCode} was terminated.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostMaintainAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _updateOrgDept.ExecuteAsync(
                    MaintainWorkforceMemberCode,
                    MaintainOrganizationCode,
                    MaintainDepartmentCode,
                    cancellationToken)
                .ConfigureAwait(false);
            await _updateManager.ExecuteAsync(
                    MaintainWorkforceMemberCode,
                    MaintainResponsibleManagerWorkforceMemberCode,
                    cancellationToken)
                .ConfigureAwait(false);
            await _updateType.ExecuteAsync(
                    MaintainWorkforceMemberCode,
                    MaintainWorkforceType,
                    cancellationToken)
                .ConfigureAwait(false);
            SuccessMessage = $"Workforce member {MaintainWorkforceMemberCode} was updated.";
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
        Members = await _listMembers.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IsBootstrap = Members.Count == 0;
        ManagerOptions = Members
            .Where(item => string.Equals(item.Status, "Active", StringComparison.Ordinal))
            .Select(item => new SelectListItem(item.WorkforceMemberCode, item.WorkforceMemberCode))
            .ToArray();
        ActiveMemberOptions = ManagerOptions;
    }
}
