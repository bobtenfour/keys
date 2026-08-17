using KeyInventory.Application.Catalog;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Lookup;

/// <summary>
/// Physical key copy row eligible for issue (active, not currently on an open loan).
/// KeyAsset identity is resolved by KEY # + MEDECO when the operator picks a row.
/// </summary>
public sealed record IssuablePhysicalCopyItem(
    string KeyNumber,
    string MedecoKeyCode,
    KeyAccessClassification Classification,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

/// <summary>
/// Bounded browse + search of physical key copies that are currently issuable
/// (active, not on an open loan). Empty query returns the first
/// <see cref="DefaultMaxResults"/> issuable copies ordered by KEY # / MEDECO.
/// </summary>
public interface ISearchIssuablePhysicalCopiesUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<IssuablePhysicalCopyItem>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public sealed class SearchIssuablePhysicalCopiesUseCase : ISearchIssuablePhysicalCopiesUseCase
{
    private readonly ISearchAvailableKeyCopiesUseCase _available;

    public SearchIssuablePhysicalCopiesUseCase(ISearchAvailableKeyCopiesUseCase available)
    {
        _available = available ?? throw new ArgumentNullException(nameof(available));
    }

    public async Task<IReadOnlyList<IssuablePhysicalCopyItem>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        int bound = maxResults < 1
            ? ISearchIssuablePhysicalCopiesUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchIssuablePhysicalCopiesUseCase.DefaultMaxResults);

        IReadOnlyList<AvailableKeyCopyCandidate> available = await _available
            .ExecuteAsync(searchText ?? string.Empty, bound, cancellationToken)
            .ConfigureAwait(false);

        return available
            .Select(item => new IssuablePhysicalCopyItem(
                item.KeyNumber,
                item.MedecoKeyCode,
                item.Classification,
                item.OpenedRooms))
            .ToArray();
    }
}
