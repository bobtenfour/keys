using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Catalog;

public interface IKeySeriesLookupPort
{
    ValueTask<KeySeries?> FindBySeriesCodeAsync(
        string seriesCode,
        CancellationToken cancellationToken);
}
