using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

public interface IKeyTypeLookupPort
{
    ValueTask<KeyType?> FindByTypeCodeAsync(
        string typeCode,
        CancellationToken cancellationToken);
}
