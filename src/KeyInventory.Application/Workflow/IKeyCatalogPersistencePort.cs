using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public interface IKeyCatalogPersistencePort
{
    Task<bool> KeyAccessPatternExistsAsync(string keyNumber, CancellationToken cancellationToken);

    Task<KeyAccessPattern?> FindKeyAccessPatternAsync(string keyNumber, CancellationToken cancellationToken);

    Task AddKeyAccessPatternAsync(KeyAccessPattern pattern, CancellationToken cancellationToken);

    Task UpdateKeyAccessPatternAsync(KeyAccessPattern pattern, CancellationToken cancellationToken);

    Task<IReadOnlyList<KeyAccessPatternListItem>> ListKeyAccessPatternsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Bounded active KEY # search for registration under an existing KEY #.
    /// Returns Classification alongside each pattern.
    /// </summary>
    Task<IReadOnlyList<KeyAccessPatternListItem>> SearchActiveKeyAccessPatternsAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    Task<bool> MedecoExistsUnderPatternAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken);

    Task AddKeyAssetAsync(KeyAsset keyAsset, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically persists a new KEY # and its first key.
    /// Regular patterns carry RoomCode on the pattern; Master carries null RoomCode.
    /// Failure must not leave an orphan KEY # without its first key.
    /// </summary>
    Task AddNewKeyNumberWithFirstKeyAsync(
        KeyAccessPattern pattern,
        KeyAsset firstKey,
        CancellationToken cancellationToken);

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
