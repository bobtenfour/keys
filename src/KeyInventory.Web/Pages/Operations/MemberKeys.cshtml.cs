using KeyInventory.Application.Lookup;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Operations;

public sealed class MemberKeysModel : PageModel
{
    private readonly IOperationalKeyLookupUseCase _lookup;

    public MemberKeysModel(IOperationalKeyLookupUseCase lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public string? WorkforceMemberCode { get; private set; }

    public IReadOnlyList<IssuedKeyForMemberItem> IssuedKeys { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? member, CancellationToken cancellationToken)
    {
        WorkforceMemberCode = member;
        if (string.IsNullOrWhiteSpace(member))
        {
            ErrorMessage = "Select a workforce member to view currently issued keys.";
            return;
        }

        try
        {
            IssuedKeys = await _lookup
                .ListIssuedKeysForWorkforceMemberAsync(member, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = exception.Message;
        }
    }
}
