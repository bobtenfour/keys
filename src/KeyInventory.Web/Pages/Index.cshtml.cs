using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IOperationalKeyLookupUseCase _lookup;
    private readonly IListKeyAssetsUseCase _listKeyAssets;

    public IndexModel(
        IOperationalKeyLookupUseCase lookup,
        IListKeyAssetsUseCase listKeyAssets)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
    }

    public int ActiveLoanCount { get; private set; }
    public int OverdueCount { get; private set; }
    public int KeysAvailableCount { get; private set; }
    public IReadOnlyList<OperationsActivityItem> RecentActivity { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationalLoanDisplay> openLoans =
            await _lookup.ListOpenLoansWithHoldersAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<OperationalLoanDisplay> returnedLoans =
            await _lookup.ListReturnedLoansWithHoldersAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        HashSet<Guid> issuedKeyAssetIds = openLoans
            .Select(loan => loan.KeyAssetId)
            .ToHashSet();

        ActiveLoanCount = openLoans.Count;
        OverdueCount = openLoans.Count(loan => loan.DueAtUtc < now);
        KeysAvailableCount = keys.Count(key =>
            key.Condition == KeyPhysicalCondition.Active
            && !issuedKeyAssetIds.Contains(key.KeyAssetId));

        RecentActivity = openLoans
            .Select(loan => new OperationsActivityItem(
                $"Issued {PartyHolderDisplayFormatter.FormatKeyCopy(loan.KeyNumber, loan.MedecoKeyCode)} to {PartyHolderDisplayFormatter.Format(loan.HolderFirstName, loan.HolderLastName, loan.HolderUin)}",
                loan.IssuedAtUtc,
                loan.DueAtUtc < now ? "Overdue" : "Attention",
                "Issued"))
            .Concat(returnedLoans.Select(loan => new OperationsActivityItem(
                $"Received {PartyHolderDisplayFormatter.FormatKeyCopy(loan.KeyNumber, loan.MedecoKeyCode)} from {PartyHolderDisplayFormatter.Format(loan.HolderFirstName, loan.HolderLastName, loan.HolderUin)}",
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
