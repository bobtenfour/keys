namespace KeyInventory.Application.Workforce;

public interface ISearchActiveWorkforceMembersUseCase
{
    const int DefaultMaxResults = 25;

    /// <summary>
    /// Bounded name/UIN search of active workforce members. Empty query returns the first
    /// <see cref="DefaultMaxResults"/> active members ordered by name.
    /// </summary>
    Task<IReadOnlyList<EligibleKeyHolderCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public sealed class SearchActiveWorkforceMembersUseCase : ISearchActiveWorkforceMembersUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public SearchActiveWorkforceMembersUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task<IReadOnlyList<EligibleKeyHolderCandidate>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        int bound = maxResults < 1
            ? ISearchActiveWorkforceMembersUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchActiveWorkforceMembersUseCase.DefaultMaxResults);

        string term = (searchText ?? string.Empty).Trim();
        return await _workforce
            .SearchActiveWorkforceMembersAsync(term, bound, cancellationToken)
            .ConfigureAwait(false);
    }
}
