using KeyInventory.Application.Workforce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration.WorkforceMembers;

public sealed class IndexModel : PageModel
{
    private readonly IListWorkforceMembersUseCase _listMembers;

    public IndexModel(IListWorkforceMembersUseCase listMembers)
    {
        _listMembers = listMembers ?? throw new ArgumentNullException(nameof(listMembers));
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public IReadOnlyList<WorkforceMemberListItem> Members { get; private set; } = [];

    public string? SuccessMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        IReadOnlyList<WorkforceMemberListItem> all = await _listMembers.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

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
                || Contains(item.DepartmentCode, query)
                || Contains(item.WorkforceType, query)
                || Contains(item.Status, query)
                || Contains($"{item.FirstName} {item.LastName}", query))
            .ToArray();
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrEmpty(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
