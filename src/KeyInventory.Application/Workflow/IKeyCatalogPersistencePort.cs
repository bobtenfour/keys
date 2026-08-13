using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public interface IKeyCatalogPersistencePort
{
    Task<bool> KeyAccessPatternExistsAsync(string keyNumber, CancellationToken cancellationToken);

    Task<KeyAccessPattern?> FindKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken);

    Task AddKeyAccessPatternAsync(KeyAccessPattern pattern, CancellationToken cancellationToken);

    Task UpdateKeyAccessPatternAsync(KeyAccessPattern pattern, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAccessPatternListItem>> ListKeyAccessPatternsAsync(CancellationToken cancellationToken);

    Task<bool> MedecoExistsUnderPatternAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken);

    Task<KeyType?> FindKeyTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task AddKeyTypeAsync(KeyType keyType, CancellationToken cancellationToken);

    Task UpdateKeyTypeAsync(KeyType keyType, CancellationToken cancellationToken);

    Task DeleteKeyTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task<int> CountActiveKeyAccessPatternsForTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task<int> CountKeyAccessPatternsForTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task<int> CountAllKeyAccessPatternsForTypeAsync(string typeCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyTypeListItem>> ListKeyTypesAsync(CancellationToken cancellationToken);

    Task AddKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken);

    Task UpdateKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken);

    Task DeleteKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task<int> CountKeyAssetsForKeyNumberAsync(string keyNumber, CancellationToken cancellationToken);

    Task DeleteKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken);

    Task<KeyAsset?> FindKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken);

    Task<KeyAsset?> FindKeyAssetAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAssetListItem>> ListKeyAssetsForPatternAsync(
        string keyNumber,
        CancellationToken cancellationToken);
}
