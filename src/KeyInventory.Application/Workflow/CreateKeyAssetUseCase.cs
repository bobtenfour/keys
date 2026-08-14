using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed class CreateKeyAssetUseCase : ICreateKeyAssetUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IOperatorAuditRecorder _audit;

    public CreateKeyAssetUseCase(IKeyCatalogPersistencePort catalog, IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public Task ExecuteAsync(
        string keyNumber,
        string medecoKeyCode,
        string typeCode,
        CancellationToken cancellationToken)
    {
        return CreateOrRegisterAsync(keyNumber, medecoKeyCode, typeCode, requireNewKeyNumber: false, cancellationToken);
    }

    public Task RegisterPhysicalCopyUnderExistingKeyNumberAsync(
        string keyNumber,
        string medecoKeyCode,
        CancellationToken cancellationToken)
    {
        return CreateOrRegisterAsync(
            keyNumber,
            medecoKeyCode,
            typeCode: null,
            requireNewKeyNumber: false,
            requireExistingKeyNumber: true,
            cancellationToken);
    }

    public Task CreateNewKeyNumberWithFirstPhysicalCopyAsync(
        string keyNumber,
        string existingTypeCode,
        string medecoKeyCode,
        CancellationToken cancellationToken)
    {
        return CreateOrRegisterAsync(
            keyNumber,
            medecoKeyCode,
            existingTypeCode,
            requireNewKeyNumber: true,
            requireExistingKeyNumber: false,
            cancellationToken);
    }

    private Task CreateOrRegisterAsync(
        string keyNumber,
        string medecoKeyCode,
        string? typeCode,
        bool requireNewKeyNumber,
        CancellationToken cancellationToken)
        => CreateOrRegisterAsync(
            keyNumber,
            medecoKeyCode,
            typeCode,
            requireNewKeyNumber,
            requireExistingKeyNumber: false,
            cancellationToken);

    private async Task CreateOrRegisterAsync(
        string keyNumber,
        string medecoKeyCode,
        string? typeCode,
        bool requireNewKeyNumber,
        bool requireExistingKeyNumber,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(medecoKeyCode);

        string normalizedKeyNumber = keyNumber.Trim();
        string normalizedMedeco = medecoKeyCode.Trim();

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(normalizedKeyNumber, cancellationToken)
            .ConfigureAwait(false);

        if (requireExistingKeyNumber && pattern is null)
        {
            throw new InvalidOperationException("The KEY # was not found. Select an existing KEY # or create a new KEY #.");
        }

        if (requireNewKeyNumber && pattern is not null)
        {
            throw new InvalidOperationException(
                "That KEY # already exists. Register a physical copy under the existing KEY # instead.");
        }

        if (pattern is null)
        {
            if (string.IsNullOrWhiteSpace(typeCode))
            {
                throw new InvalidOperationException("Select an existing Key Type for the new KEY #.");
            }

            KeyType keyType = await RequireExistingActiveKeyTypeAsync(typeCode, cancellationToken)
                .ConfigureAwait(false);
            pattern = new KeyAccessPattern(normalizedKeyNumber, keyType);
            _audit.Stage(
                OperatorAuditActions.KeyAccessPatternCreated,
                OperatorAuditSubjects.KeyAccessPattern,
                pattern.KeyNumber,
                $"typeCode={keyType.TypeCode}");
            await _catalog.AddKeyAccessPatternAsync(pattern, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (!pattern.IsActive)
            {
                throw new InvalidOperationException("An inactive KEY # cannot receive new physical copies.");
            }

            if (!string.IsNullOrWhiteSpace(typeCode)
                && !string.Equals(pattern.KeyType.TypeCode, typeCode.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Physical copies under an existing KEY # must use that KEY #'s Key Type.");
            }
        }

        if (await _catalog.MedecoExistsUnderPatternAsync(pattern.KeyNumber, normalizedMedeco, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("A MEDECO Key Code already exists under this KEY #.");
        }

        KeyAsset keyAsset = new(Guid.NewGuid(), pattern, normalizedMedeco);
        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyRegistered,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}; typeCode={pattern.KeyType.TypeCode}");
        await _catalog.AddKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }

    private async Task<KeyType> RequireExistingActiveKeyTypeAsync(string typeCode, CancellationToken cancellationToken)
    {
        KeyType? keyType = await _catalog.FindKeyTypeAsync(typeCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (keyType is null)
        {
            throw new InvalidOperationException(
                "The Key Type was not found. Create the Key Type first, then create the KEY #.");
        }

        if (!keyType.IsActive)
        {
            throw new InvalidOperationException("The key type is inactive and cannot be used for a new KEY # or copy.");
        }

        return keyType;
    }
}
