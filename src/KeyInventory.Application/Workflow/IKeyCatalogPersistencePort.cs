using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public interface IKeyCatalogPersistencePort
{
    Task<bool> KeyAssetExistsAsync(string catalogKeyCode, CancellationToken cancellationToken);

    Task<KeyType?> FindKeyTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task AddKeyTypeAsync(KeyType keyType, CancellationToken cancellationToken);

    Task UpdateKeyTypeAsync(KeyType keyType, CancellationToken cancellationToken);

    Task<int> CountActiveKeyAssetsForTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyTypeListItem>> ListKeyTypesAsync(CancellationToken cancellationToken);

    Task AddKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken);

    Task<KeyAsset?> FindKeyAssetAsync(string catalogKeyCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsAsync(CancellationToken cancellationToken);
}
