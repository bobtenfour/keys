namespace KeyInventory.Application.Workforce;

public interface ISearchActiveRoomsUseCase
{
    const int DefaultMaxResults = 25;

    /// <summary>
    /// Bounded RoomNumber/Description search of active rooms. Empty query returns the first
    /// <see cref="DefaultMaxResults"/> active rooms ordered by RoomNumber.
    /// Room results include the room's DepartmentId + DepartmentCode.
    /// </summary>
    Task<IReadOnlyList<RoomListItem>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bounded search restricted to rooms whose Department matches the given
    /// <paramref name="workforceMemberCode"/>'s Department. Enforces the
    /// Room.DepartmentId == WorkforceMember.DepartmentId Work Assignment invariant
    /// at the candidate-list boundary. Empty query returns a bounded browse of matching rooms.
    /// </summary>
    Task<IReadOnlyList<RoomListItem>> ExecuteForWorkforceMemberAsync(
        string workforceMemberCode,
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
        int bound = maxResults < 1
            ? ISearchActiveRoomsUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchActiveRoomsUseCase.DefaultMaxResults);

        string term = (searchText ?? string.Empty).Trim();
        return await _workforce
            .SearchActiveRoomsAsync(term, bound, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoomListItem>> ExecuteForWorkforceMemberAsync(
        string workforceMemberCode,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);

        int bound = maxResults < 1
            ? ISearchActiveRoomsUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchActiveRoomsUseCase.DefaultMaxResults);

        Domain.Workforce.WorkforceMember? member = await _workforce
            .FindWorkforceMemberAsync(workforceMemberCode.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            return [];
        }

        string term = (searchText ?? string.Empty).Trim();
        return await _workforce
            .SearchActiveRoomsInDepartmentAsync(member.DepartmentId, term, bound, cancellationToken)
            .ConfigureAwait(false);
    }
}
