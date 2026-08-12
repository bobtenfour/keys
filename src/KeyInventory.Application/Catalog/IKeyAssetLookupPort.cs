using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

public interface IKeyAssetLookupPort
{
    ValueTask<KeyAsset?> FindByKeyAssetIdAsync(
        Guid keyAssetId,
        CancellationToken cancellationToken);

    ValueTask<KeyAsset?> FindByKeyNumberAndMedecoAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken);
}
