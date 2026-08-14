using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;

namespace KeyInventory.Application.Lookup;

public sealed record AvailableKeyCopyCandidate(
    string KeyNumber,
    string MedecoKeyCode,
    string TypeCode,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public interface ISearchAvailableKeyCopiesUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<AvailableKeyCopyCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    Task<AvailableKeyCopyCandidate?> FindAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AvailableKeyCopyCandidate>> ListAvailableForKeyNumberAsync(
        string keyNumber,
        CancellationToken cancellationToken);

    Task<bool> HasAnyAvailableAsync(CancellationToken cancellationToken);
}

public sealed class SearchAvailableKeyCopiesUseCase : ISearchAvailableKeyCopiesUseCase
{
    private readonly IListKeyAssetsUseCase _listKeyAssets;
    private readonly IListOpenLoansUseCase _listOpenLoans;

    public SearchAvailableKeyCopiesUseCase(
        IListKeyAssetsUseCase listKeyAssets,
        IListOpenLoansUseCase listOpenLoans)
    {
        _listKeyAssets = listKeyAssets ?? throw new ArgumentNullException(nameof(listKeyAssets));
        _listOpenLoans = listOpenLoans ?? throw new ArgumentNullException(nameof(listOpenLoans));
    }

    public async Task<bool> HasAnyAvailableAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AvailableKeyCopyCandidate> all = await ListAvailableInternalAsync(cancellationToken)
            .ConfigureAwait(false);
        return all.Count > 0;
    }

    public async Task<IReadOnlyList<AvailableKeyCopyCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        int bound = maxResults < 1
            ? ISearchAvailableKeyCopiesUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchAvailableKeyCopiesUseCase.DefaultMaxResults);

        string term = searchText.Trim();
        IReadOnlyList<AvailableKeyCopyCandidate> available = await ListAvailableInternalAsync(cancellationToken)
            .ConfigureAwait(false);

        return available
            .Where(item =>
                item.KeyNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || item.MedecoKeyCode.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.KeyNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.MedecoKeyCode, StringComparer.OrdinalIgnoreCase)
            .Take(bound)
            .ToArray();
    }

    public async Task<AvailableKeyCopyCandidate?> FindAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyNumber) || string.IsNullOrWhiteSpace(medecoKeyCode))
        {
            return null;
        }

        IReadOnlyList<AvailableKeyCopyCandidate> available = await ListAvailableInternalAsync(cancellationToken)
            .ConfigureAwait(false);
        return available.FirstOrDefault(item =>
            string.Equals(item.KeyNumber, keyNumber.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.MedecoKeyCode, medecoKeyCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<AvailableKeyCopyCandidate>> ListAvailableForKeyNumberAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyNumber))
        {
            return [];
        }

        IReadOnlyList<AvailableKeyCopyCandidate> available = await ListAvailableInternalAsync(cancellationToken)
            .ConfigureAwait(false);
        return available
            .Where(item => string.Equals(item.KeyNumber, keyNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.MedecoKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<AvailableKeyCopyCandidate>> ListAvailableInternalAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyAssetListItem> keys = await _listKeyAssets.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<LoanListItem> openItems = await _listOpenLoans.ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        HashSet<Guid> issued = openItems.Select(item => item.KeyAssetId).ToHashSet();

        return keys
            .Where(key => key.IsActive && !issued.Contains(key.KeyAssetId))
            .Select(key => new AvailableKeyCopyCandidate(
                key.KeyNumber,
                key.MedecoKeyCode,
                key.TypeCode,
                key.OpenedRooms))
            .ToArray();
    }
}
