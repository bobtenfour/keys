using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

public interface IKeyAssetLookupPort
{
    ValueTask<KeyAsset?> FindByCatalogKeyCodeAsync(
        string catalogKeyCode,
        CancellationToken cancellationToken);
}
