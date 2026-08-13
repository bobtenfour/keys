namespace KeyInventory.Application.Lookup;

/// <summary>
/// Application-owned global operator search orchestration for the header search.
/// Coordinates existing person, room, KEY #, MEDECO, and custody authorities.
/// </summary>
public interface IGlobalOperatorSearchUseCase
{
    public const int DefaultMaxPerCategory = 25;

    Task<GlobalOperatorSearchResult> SearchAsync(string query, CancellationToken cancellationToken);
}
