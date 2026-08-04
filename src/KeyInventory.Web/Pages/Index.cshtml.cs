using KeyInventory.Application.Workflow;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IListOpenLoansUseCase _listOpenLoans;
    private readonly IListReturnedLoansUseCase _listReturnedLoans;
    private readonly IListKeyAssetsUseCase _listKeyAssets;

    public IndexModel(
        IListOpenLoansUseCase listOpenLoans,
        IListReturnedLoansUseCase listReturnedLoans,
        IListKeyAssetsUseCase listKeyAssets)
    {
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
        _listReturnedLoans = listReturnedLoans ?? throw new ArgumentNullException(nameof(listReturnedLoans));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
    }

    public int ActiveLoanCount { get; private set; }
    public int OverdueCount { get; private set; }
    public int KeysAvailableCount { get; private set; }
    public IReadOnlyList<OperationsActivityItem> RecentActivity { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<LoanListItem> openLoans = await _listOpenLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<LoanListItem> returnedLoans = await _listReturnedLoans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        HashSet<string> issuedKeyCodes = openLoans
            .Select(loan => loan.CatalogKeyCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        ActiveLoanCount = openLoans.Count;
        OverdueCount = openLoans.Count(loan => loan.DueAtUtc < now);
        KeysAvailableCount = keys.Count(key => key.IsActive && !issuedKeyCodes.Contains(key.CatalogKeyCode));

        RecentActivity = openLoans
            .Select(loan => new OperationsActivityItem(
                $"Issued Key {loan.CatalogKeyCode} to {loan.BorrowerPartyReference}",
                loan.IssuedAtUtc,
                loan.DueAtUtc < now ? "Overdue" : "Attention",
                "Issued"))
            .Concat(returnedLoans.Select(loan => new OperationsActivityItem(
                $"Received Key {loan.CatalogKeyCode} from {loan.BorrowerPartyReference}",
                loan.ReturnedAtUtc ?? loan.IssuedAtUtc,
                "Success",
                "Received")))
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(6)
            .ToArray();
    }
}

public sealed record OperationsActivityItem(
    string Description,
    DateTimeOffset OccurredAtUtc,
    string BadgeKind,
    string Kind);