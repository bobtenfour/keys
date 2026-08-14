using KeyInventory.Application.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace KeyInventory.ArchitectureTests;

internal static class CatalogSeedHelper
{
    public static async Task CreateKeyTypeIfMissingAsync(
        IServiceProvider services,
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        IListKeyTypesUseCase list = services.GetRequiredService<IListKeyTypesUseCase>();
        IReadOnlyList<KeyTypeListItem> types = await list.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (types.Any(item => string.Equals(item.TypeCode, typeCode, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await services.GetRequiredService<ICreateKeyTypeUseCase>()
            .ExecuteAsync(typeCode, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task CreatePhysicalKeyAsync(
        IServiceProvider services,
        string keyNumber,
        string medecoKeyCode,
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        await CreateKeyTypeIfMissingAsync(services, typeCode, cancellationToken).ConfigureAwait(false);
        await services.GetRequiredService<ICreateKeyAssetUseCase>()
            .ExecuteAsync(keyNumber, medecoKeyCode, typeCode, cancellationToken)
            .ConfigureAwait(false);
    }
}
