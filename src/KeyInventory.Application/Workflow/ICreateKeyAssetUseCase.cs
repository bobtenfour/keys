namespace KeyInventory.Application.Workflow;

/// <summary>
/// Registers a physical MEDECO copy under a KEY #.
/// Existing KEY # registration derives Key Type from the KEY #.
/// New KEY # creation requires an existing Key Type (no silent Key Type creation).
/// </summary>
public interface ICreateKeyAssetUseCase
{
    /// <summary>
    /// Creates a new KEY # with the first physical MEDECO copy, or registers a copy under an existing KEY #
    /// when the KEY # already exists and the type matches. Requires an existing Key Type.
    /// Prefer <see cref="RegisterPhysicalCopyUnderExistingKeyNumberAsync"/> or
    /// <see cref="CreateNewKeyNumberWithFirstPhysicalCopyAsync"/> for explicit operator intent.
    /// </summary>
    Task ExecuteAsync(
        string keyNumber,
        string medecoKeyCode,
        string typeCode,
        CancellationToken cancellationToken);

    Task RegisterPhysicalCopyUnderExistingKeyNumberAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken);

    Task CreateNewKeyNumberWithFirstPhysicalCopyAsync(
        string keyNumber,
        string existingTypeCode,
        string medecoKeyCode,
        CancellationToken cancellationToken);
}
