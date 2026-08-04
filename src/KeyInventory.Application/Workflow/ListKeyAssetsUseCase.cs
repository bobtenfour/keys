namespace KeyInventory.Application.Workflow;

public sealed class ListKeyAssetsUseCase : IListKeyAssetsUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;

    public ListKeyAssetsUseCase(IKeyCatalogPersistencePort catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public Task<IReadOnlyList<KeyAssetListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _catalog.ListKeyAssetsAsync(cancellationToken);
    }
}
