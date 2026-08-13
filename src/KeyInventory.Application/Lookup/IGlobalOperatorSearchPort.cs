namespace KeyInventory.Application.Lookup;

/// <summary>
/// Persistence-backed queries for global operator search. Infrastructure composes existing
/// authorities; this port does not introduce a second search store.
/// </summary>
public interface IGlobalOperatorSearchPort
{
    Task<GlobalOperatorSearchResult> SearchAsync(
        string query,
        int maxPerCategory,
        CancellationToken cancellationToken);
}
