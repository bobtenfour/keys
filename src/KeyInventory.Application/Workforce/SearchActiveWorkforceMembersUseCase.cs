namespace KeyInventory.Application.Workforce;

public interface ISearchActiveWorkforceMembersUseCase
{
    const int DefaultMaxResults = 25;

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
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        int bound = maxResults < 1
            ? ISearchActiveWorkforceMembersUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchActiveWorkforceMembersUseCase.DefaultMaxResults);

        return await _workforce
            .SearchActiveWorkforceMembersAsync(searchText.Trim(), bound, cancellationToken)
            .ConfigureAwait(false);
    }
}
