namespace KeyInventory.Domain.Catalog;

public sealed class KeySeries
{
    public KeySeries(string seriesCode)
    {
        SeriesCode = CatalogText.Require(seriesCode, nameof(seriesCode));
        IsActive = true;
    }

    public string SeriesCode { get; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Retire(bool hasActiveKeyAssets)
    {
        if (hasActiveKeyAssets)
        {
            throw new InvalidOperationException(
                "KeySeries cannot be retired while active KeyAsset records reference it for new catalog assignment.");
        }

        IsActive = false;
    }
}
