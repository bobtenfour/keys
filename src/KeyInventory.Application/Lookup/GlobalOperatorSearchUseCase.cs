namespace KeyInventory.Application.Lookup;

public sealed class GlobalOperatorSearchUseCase : IGlobalOperatorSearchUseCase
{
    private readonly IGlobalOperatorSearchPort _search;

    public GlobalOperatorSearchUseCase(IGlobalOperatorSearchPort search)
    {
        _search = search ?? throw new ArgumentNullException(nameof(search));
    }

    public Task<GlobalOperatorSearchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new GlobalOperatorSearchResult(
                string.Empty,
                [],
                [],
                [],
                []));
        }

        string normalized = query.Trim();
        return _search.SearchAsync(
            normalized,
            IGlobalOperatorSearchUseCase.DefaultMaxPerCategory,
            cancellationToken);
    }
}
