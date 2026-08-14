namespace KeyInventory.Application.Workforce;

public interface ISearchActiveRoomsUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<RoomListItem>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public sealed class SearchActiveRoomsUseCase : ISearchActiveRoomsUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public SearchActiveRoomsUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task<IReadOnlyList<RoomListItem>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        int bound = maxResults < 1
            ? ISearchActiveRoomsUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchActiveRoomsUseCase.DefaultMaxResults);

        return await _workforce
            .SearchActiveRoomsAsync(searchText.Trim(), bound, cancellationToken)
            .ConfigureAwait(false);
    }
}
