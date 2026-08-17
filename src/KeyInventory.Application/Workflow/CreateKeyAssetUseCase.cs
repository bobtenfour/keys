using KeyInventory.Application.Catalog;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Locations;

namespace KeyInventory.Application.Workflow;

public sealed class CreateKeyAssetUseCase : ICreateKeyAssetUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IWorkforcePersistencePort _workforce;
    private readonly IOperatorAuditRecorder _audit;

    public CreateKeyAssetUseCase(
        IKeyCatalogPersistencePort catalog,
        IWorkforcePersistencePort workforce,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<RegisterNewKeyResult> RegisterNewKeyAsync(
        string keyNumber,
        string medecoKeyCode,
        KeyAccessClassification? classification,
        IReadOnlyList<string>? roomCodes,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(medecoKeyCode);

        string normalizedKeyNumber = keyNumber.Trim();
        string normalizedMedeco = medecoKeyCode.Trim();
        IReadOnlyList<string> normalizedRooms = NormalizeRoomCodes(roomCodes);

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(normalizedKeyNumber, cancellationToken)
            .ConfigureAwait(false);

        if (pattern is not null)
        {
            return await RegisterUnderExistingAsync(
                    pattern,
                    normalizedMedeco,
                    classification,
                    normalizedRooms,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await CreateNewKeyNumberAtomicallyAsync(
                normalizedKeyNumber,
                normalizedMedeco,
                classification,
                normalizedRooms,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<RegisterNewKeyResult> RegisterUnderExistingAsync(
        KeyAccessPattern pattern,
        string normalizedMedeco,
        KeyAccessClassification? classification,
        IReadOnlyList<string> roomCodes,
        CancellationToken cancellationToken)
    {
        if (classification is not null)
        {
            throw new InvalidOperationException(
                "Classification cannot be changed when registering a key under an existing KEY #.");
        }

        if (roomCodes.Count > 0)
        {
            throw new InvalidOperationException(
                "Room cannot be changed when registering a key under an existing KEY #.");
        }

        if (!pattern.IsActive)
        {
            throw new InvalidOperationException("An inactive KEY # cannot receive new keys.");
        }

        if (await _catalog.MedecoExistsUnderPatternAsync(pattern.KeyNumber, normalizedMedeco, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("That MEDECO already exists under this KEY #.");
        }

        KeyAsset keyAsset = new(Guid.NewGuid(), pattern, normalizedMedeco);
        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyRegistered,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KEY#={keyAsset.KeyNumber}; MEDECO={keyAsset.MedecoKeyCode}; classification={pattern.Classification}");
        await _catalog.AddKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);

        return new RegisterNewKeyResult(pattern.KeyNumber, keyAsset.MedecoKeyCode, CreatedNewKeyNumber: false);
    }

    private async Task<RegisterNewKeyResult> CreateNewKeyNumberAtomicallyAsync(
        string normalizedKeyNumber,
        string normalizedMedeco,
        KeyAccessClassification? classification,
        IReadOnlyList<string> roomCodes,
        CancellationToken cancellationToken)
    {
        if (classification is null
            || classification is not (KeyAccessClassification.Regular or KeyAccessClassification.Master))
        {
            throw new InvalidOperationException(
                "Select Regular or Master. Classification is required when the KEY # does not yet exist.");
        }

        string? roomCode = null;
        if (classification == KeyAccessClassification.Regular)
        {
            if (roomCodes.Count != 1)
            {
                throw new InvalidOperationException(
                    "Regular KEY # requires exactly one Room.");
            }

            roomCode = roomCodes[0];
            Room? room = await _workforce.FindRoomAsync(roomCode, cancellationToken).ConfigureAwait(false);
            if (room is null || !room.IsActive)
            {
                throw new InvalidOperationException($"Room '{roomCode}' was not found or is not active.");
            }

            roomCode = room.RoomCode;
        }
        else if (roomCodes.Count > 0)
        {
            throw new InvalidOperationException(
                "Master KEY # cannot have a Room. Access derives all Rooms from Classification.");
        }

        KeyAccessPattern pattern = new(normalizedKeyNumber, classification.Value, roomCode);
        KeyAsset keyAsset = new(Guid.NewGuid(), pattern, normalizedMedeco);

        _audit.Stage(
            OperatorAuditActions.KeyAccessPatternCreated,
            OperatorAuditSubjects.KeyAccessPattern,
            pattern.KeyNumber,
            roomCode is null
                ? $"classification={pattern.Classification}; access=All Rooms"
                : $"classification={pattern.Classification}; Room={roomCode}");

        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyRegistered,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KEY#={keyAsset.KeyNumber}; MEDECO={keyAsset.MedecoKeyCode}; classification={pattern.Classification}");

        await _catalog
            .AddNewKeyNumberWithFirstKeyAsync(pattern, keyAsset, cancellationToken)
            .ConfigureAwait(false);

        return new RegisterNewKeyResult(pattern.KeyNumber, keyAsset.MedecoKeyCode, CreatedNewKeyNumber: true);
    }

    private static List<string> NormalizeRoomCodes(IReadOnlyList<string>? roomCodes)
    {
        if (roomCodes is null || roomCodes.Count == 0)
        {
            return [];
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> normalized = [];
        foreach (string raw in roomCodes)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            string trimmed = raw.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized;
    }
}
