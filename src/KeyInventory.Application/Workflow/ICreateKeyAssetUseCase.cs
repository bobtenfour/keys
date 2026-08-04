namespace KeyInventory.Application.Workflow;

public interface ICreateKeyAssetUseCase
{
    Task ExecuteAsync(string catalogKeyCode, string typeCode, CancellationToken cancellationToken);
}
