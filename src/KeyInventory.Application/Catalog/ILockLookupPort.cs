using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

public interface ILockLookupPort
{
    ValueTask<Lock?> FindByLockCodeAsync(
        string lockCode,
        CancellationToken cancellationToken);
}
