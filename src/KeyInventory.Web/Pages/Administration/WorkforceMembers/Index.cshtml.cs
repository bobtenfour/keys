using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class IndexModel : PageModel
{
    private readonly IListWorkforceMembersUseCase _listMembers;
    private Dictionary<string, WorkforceMemberListItem> _byCode =
        new(StringComparer.OrdinalIgnoreCase);

    public IndexModel(IListWorkforceMembersUseCase listMembers)
    {
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public IReadOnlyList<WorkforceMemberListItem> Members { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkforceMemberListItem> all = await _listMembers.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        _byCode = all.ToDictionary(item => item.WorkforceMemberCode, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(Q))
        {
            Members = all;
            return;
        }

        string query = Q.Trim();
        Members = all
            .Where(item =>
                Contains(item.FirstName, query)
                || Contains(item.LastName, query)
                || Contains(item.Uin, query)
                || Contains(item.OrganizationCode, query)
                || Contains(item.DepartmentCode, query)
                || Contains(item.WorkforceType, query)
                || Contains(item.Status, query)
                || Contains($"{item.FirstName} {item.LastName}", query))
            .ToArray();
    }

    public string FormatManager(string managerCode)
    {
        if (_byCode.TryGetValue(managerCode, out WorkforceMemberListItem? manager))
        {
            return $"{manager.FirstName} {manager.LastName}";
        }

        return managerCode;
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrEmpty(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
