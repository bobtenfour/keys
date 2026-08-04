namespace KeyInventory.Application.Workflow;

public interface IListKeyAssetsUseCase
{
    Task<IReadOnlyList<KeyAssetListItem>> ExecuteAsync(CancellationToken cancellationToken);
}
