using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

/// <summary>
/// Registers a new key under Application authority. Creating a key is always one
/// business operation that requires KEY # and MEDECO on the same request. Application
/// resolves whether the KEY # already exists; the operator does not choose modes.
/// </summary>
public interface ICreateKeyAssetUseCase
{
    /// <summary>
    /// Single New Key operation.
    /// When the KEY # exists: creates only a new KeyAsset under that KEY # (Classification/Rooms unchanged).
    /// When the KEY # does not exist: atomically creates the KEY # with Classification and Rooms
    /// together with its first KeyAsset. Failure must not leave an orphan KEY #.
    /// Classification and roomCodes are required only when the KEY # does not yet exist;
    /// they are rejected when the KEY # already exists.
    /// </summary>
    Task<RegisterNewKeyResult> RegisterNewKeyAsync(
        string keyNumber,
        string medecoKeyCode,
        KeyAccessClassification? classification,
        IReadOnlyList<string>? roomCodes,
        CancellationToken cancellationToken);
}

/// <summary>
/// Outcome of a successful New Key registration.
/// </summary>
public sealed record RegisterNewKeyResult(
    string KeyNumber,
    string MedecoKeyCode,
    bool CreatedNewKeyNumber);
