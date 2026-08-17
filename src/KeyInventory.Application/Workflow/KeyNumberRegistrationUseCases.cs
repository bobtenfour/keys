using KeyInventory.Application.Catalog;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed record KeyNumberRegistrationPreview(
    string KeyNumber,
    KeyAccessClassification Classification,
    bool IsActive,
    IReadOnlyList<KeyOpenedRoomItem> OpenedRooms);

public interface IGetKeyNumberRegistrationPreviewUseCase
{
    Task<KeyNumberRegistrationPreview?> ExecuteAsync(string keyNumber, CancellationToken cancellationToken);
}

public interface ISearchKeyNumbersForRegistrationUseCase
{
    const int DefaultMaxResults = 25;

    Task<IReadOnlyList<KeyNumberRegistrationPreview>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);
}

public sealed class GetKeyNumberRegistrationPreviewUseCase : IGetKeyNumberRegistrationPreviewUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IKeyAccessResolutionPort _accessResolution;

    public GetKeyNumberRegistrationPreviewUseCase(
        IKeyCatalogPersistencePort catalog,
        IKeyAccessResolutionPort accessResolution)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _accessResolution = accessResolution ?? throw new ArgumentNullException(nameof(accessResolution));
    }

    public async Task<KeyNumberRegistrationPreview?> ExecuteAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyNumber))
        {
            return null;
        }

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(keyNumber.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            return null;
        }

        IReadOnlyList<KeyOpenedRoomItem> rooms = await _accessResolution
            .ResolveForKeyNumberAsync(pattern.KeyNumber, expandMaster: false, cancellationToken)
            .ConfigureAwait(false);
        return new KeyNumberRegistrationPreview(
            pattern.KeyNumber,
            pattern.Classification,
            pattern.IsActive,
            rooms);
    }
}

public sealed class SearchKeyNumbersForRegistrationUseCase : ISearchKeyNumbersForRegistrationUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;

    public SearchKeyNumbersForRegistrationUseCase(IKeyCatalogPersistencePort catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public async Task<IReadOnlyList<KeyNumberRegistrationPreview>> ExecuteAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        int bound = maxResults < 1
            ? ISearchKeyNumbersForRegistrationUseCase.DefaultMaxResults
            : Math.Min(maxResults, ISearchKeyNumbersForRegistrationUseCase.DefaultMaxResults);

        string term = (searchText ?? string.Empty).Trim();
        IReadOnlyList<KeyAccessPatternListItem> patterns = await _catalog
            .SearchActiveKeyAccessPatternsAsync(term, bound, cancellationToken)
            .ConfigureAwait(false);

        return patterns
            .Select(pattern => new KeyNumberRegistrationPreview(
                pattern.KeyNumber,
                pattern.Classification,
                pattern.IsActive,
                pattern.OpenedRooms))
            .ToArray();
    }
}
