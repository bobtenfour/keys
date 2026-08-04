using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed class CreateKeyAssetUseCase : ICreateKeyAssetUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;

    public CreateKeyAssetUseCase(IKeyCatalogPersistencePort catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public async Task ExecuteAsync(string catalogKeyCode, string typeCode, CancellationToken cancellationToken)
    {
        if (await _catalog.KeyAssetExistsAsync(catalogKeyCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A key with this catalog code already exists.");
        }

        KeyType? keyType = await _catalog.FindKeyTypeAsync(typeCode, cancellationToken).ConfigureAwait(false);
        if (keyType is null)
        {
            keyType = new KeyType(typeCode);
            await _catalog.AddKeyTypeAsync(keyType, cancellationToken).ConfigureAwait(false);
        }
        else if (!keyType.IsActive)
        {
            throw new InvalidOperationException("The key type is inactive and cannot be used for a new key.");
        }

        KeyAsset keyAsset = new(catalogKeyCode, keyType);
        await _catalog.AddKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }
}
