using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

public interface ILocationLookupPort
{
    ValueTask<Location?> FindByLocationCodeAsync(
        string locationCode,
        CancellationToken cancellationToken);
}
