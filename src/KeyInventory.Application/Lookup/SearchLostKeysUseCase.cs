using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Lookup;

public sealed record LostKeyCandidate(
    Guid KeyAssetId,
    string KeyNumber,
    string MedecoKeyCode,
    KeyAccessClassification Classification,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public interface ISearchLostKeysUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<LostKeyCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public sealed class SearchLostKeysUseCase : ISearchLostKeysUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;

    public SearchLostKeysUseCase(IKeyCatalogPersistencePort catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public async Task<IReadOnlyList<LostKeyCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        int bound = maxResults < 1
            ? ISearchLostKeysUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchLostKeysUseCase.DefaultMaxResults);

        string term = (searchText ?? string.Empty).Trim();
        IReadOnlyList<KeyAssetListItem> all = await _catalog.ListKeyAssetsAsync(cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<KeyAssetListItem> lost = all
            .Where(key => key.Condition == KeyPhysicalCondition.Lost)
            .OrderBy(key => key.KeyNumber)
            .ThenBy(key => key.MedecoKeyCode);

        if (term.Length > 0)
        {
            lost = lost.Where(key =>
                key.KeyNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || key.MedecoKeyCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return lost
            .Take(bound)
            .Select(key => new LostKeyCandidate(
                key.KeyAssetId,
                key.KeyNumber,
                key.MedecoKeyCode,
                key.Classification,
                key.OpenedRooms))
            .ToArray();
    }
}
