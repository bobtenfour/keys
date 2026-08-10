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

    public async Task ExecuteAsync(string catalogKeyCode, string typeCode, CancellationToken cancellationToken)
    {
        if (await _catalog.KeyAssetExistsAsync(catalogKeyCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A key with this catalog code already exists.");
        }

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
            throw new InvalidOperationException("The key type is inactive and cannot be used for a new key.");
        }

        KeyAsset keyAsset = new(catalogKeyCode, keyType);
        _audit.Stage(
            OperatorAuditActions.KeyRegistered,
            OperatorAuditSubjects.Key,
            keyAsset.CatalogKeyCode,
            $"typeCode={keyType.TypeCode}");
        await _catalog.AddKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }
}
