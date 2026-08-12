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

    public async Task ExecuteAsync(
        string keyNumber,
        string medecoKeyCode,
        string typeCode,
        CancellationToken cancellationToken)
    {
        KeyType? keyType = await _catalog.FindKeyTypeAsync(typeCode, cancellationToken).ConfigureAwait(false);
        if (keyType is null)
        {
            keyType = new KeyType(typeCode);
            _audit.Stage(
                OperatorAuditActions.KeyTypeCreated,
                OperatorAuditSubjects.KeyType,
                keyType.TypeCode);
            await _catalog.AddKeyTypeAsync(keyType, cancellationToken).ConfigureAwait(false);
        }
        else if (!keyType.IsActive)
        {
            throw new InvalidOperationException("The key type is inactive and cannot be used for a new KEY # or copy.");
        }

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(keyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null)
        {
            pattern = new KeyAccessPattern(keyNumber, keyType);
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

            if (!string.Equals(pattern.KeyType.TypeCode, keyType.TypeCode, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Physical copies under an existing KEY # must use that KEY #'s Key Type.");
            }
        }

        if (await _catalog.MedecoExistsUnderPatternAsync(pattern.KeyNumber, medecoKeyCode, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("A MEDECO Key Code already exists under this KEY #.");
        }

        KeyAsset keyAsset = new(Guid.NewGuid(), pattern, medecoKeyCode);
        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyRegistered,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}; typeCode={pattern.KeyType.TypeCode}");
        await _catalog.AddKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }
}
