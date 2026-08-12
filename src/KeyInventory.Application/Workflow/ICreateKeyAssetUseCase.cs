namespace KeyInventory.Application.Workflow;

/// <summary>
/// Registers a physical MEDECO copy under a KEY #. Creates the KEY # (and KeyType) when needed.
/// </summary>
public interface ICreateKeyAssetUseCase
{
    Task ExecuteAsync(
        string keyNumber,
        string medecoKeyCode,
        string typeCode,
        CancellationToken cancellationToken);
}
