using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class AddModel : PageModel
{
    private readonly IRegisterWorkforceMemberUseCase _register;
    private readonly IRegisterBootstrapWorkforcePairUseCase _registerBootstrap;
    private readonly IListWorkforceMembersUseCase _listMembers;
    private readonly IListOrganizationsUseCase _listOrganizations;
    private readonly IListDepartmentsUseCase _listDepartments;

    public AddModel(
        IRegisterWorkforceMemberUseCase register,
        IRegisterBootstrapWorkforcePairUseCase registerBootstrap,
        IListWorkforceMembersUseCase listMembers,
        IListOrganizationsUseCase listOrganizations,
        IListDepartmentsUseCase listDepartments)
    {
        _register = register ?? throw new ArgumentNullException(nameof(register));
        _registerBootstrap = registerBootstrap ?? throw new ArgumentNullException(nameof(registerBootstrap));
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
        _listOrganizations = listOrganizations ?? throw new ArgumentNullException(nameof(listOrganizations));
        _listDepartments = listDepartments ?? throw new ArgumentNullException(nameof(listDepartments));
    }

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public string Uin { get; set; } = string.Empty;

    [BindProperty]
    public string WorkforceType { get; set; } = "Employee";

    [BindProperty]
    public string OrganizationCode { get; set; } = string.Empty;

    [BindProperty]
    public string DepartmentCode { get; set; } = string.Empty;

    [BindProperty]
    public string ResponsibleManagerWorkforceMemberCode { get; set; } = string.Empty;

    [BindProperty]
    public string PeerFirstName { get; set; } = string.Empty;

    [BindProperty]
    public string PeerLastName { get; set; } = string.Empty;

    [BindProperty]
    public string PeerUin { get; set; } = string.Empty;

    [BindProperty]
    public string PeerWorkforceType { get; set; } = "Employee";

    public bool IsBootstrap { get; private set; }

    public IReadOnlyList<SelectListItem> OrganizationOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; private set; } = [];

    public IReadOnlyList<SelectListItem> ManagerOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<WorkforceMemberListItem> members = await _listMembers.ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            if (members.Count == 0)
            {
                await _registerBootstrap.ExecuteAsync(
                        FirstName,
                        LastName,
                        Uin,
                        WorkforceType,
                        PeerFirstName,
                        PeerLastName,
                        PeerUin,
                        PeerWorkforceType,
                        OrganizationCode,
                        DepartmentCode,
                        cancellationToken)
                    .ConfigureAwait(false);
                return RedirectToPage("./Index");
            }

            string memberCode = await _register.ExecuteAsync(
                    FirstName,
                    LastName,
                    Uin,
                    WorkforceType,
                    OrganizationCode,
                    DepartmentCode,
                    ResponsibleManagerWorkforceMemberCode,
                    cancellationToken)
                .ConfigureAwait(false);
            return RedirectToPage("./Details", new { member = memberCode });
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
        IReadOnlyList<WorkforceMemberListItem> members = await _listMembers.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        IsBootstrap = members.Count == 0;

        IReadOnlyList<OrganizationListItem> organizations = await _listOrganizations.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        OrganizationOptions = organizations
            .Where(item => item.IsActive)
            .Select(item => new SelectListItem(
                item.OrganizationCode,
                item.OrganizationCode,
                string.Equals(item.OrganizationCode, OrganizationCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        IReadOnlyList<DepartmentListItem> departments = await _listDepartments.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        DepartmentOptions = departments
            .Where(item => item.IsActive
                && (string.IsNullOrWhiteSpace(OrganizationCode)
                    || string.Equals(item.OrganizationCode, OrganizationCode, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new SelectListItem(
                $"{item.OrganizationCode} / {item.DepartmentCode}",
                item.DepartmentCode,
                string.Equals(item.DepartmentCode, DepartmentCode, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        ManagerOptions = members
            .Where(item => string.Equals(item.Status, "Active", StringComparison.Ordinal))
            .Select(item => new SelectListItem(
                PartyHolderDisplayFormatter.Format(item.FirstName, item.LastName, item.Uin),
                item.WorkforceMemberCode,
                string.Equals(
                    item.WorkforceMemberCode,
                    ResponsibleManagerWorkforceMemberCode,
                    StringComparison.Ordinal)))
            .ToArray();
    }
}
