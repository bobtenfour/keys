using KeyInventory.Application.OperatorAudit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.Application.Workflow;

public sealed record KeyTypeListItem(string TypeCode, bool IsActive, int ActiveKeyAssetCount);

public interface IListKeyTypesUseCase
{
    Task<IReadOnlyList<KeyTypeListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public interface IActivateKeyTypeUseCase
{
    Task ExecuteAsync(string typeCode, CancellationToken cancellationToken);
}

public interface IRetireKeyTypeUseCase
{
    Task ExecuteAsync(string typeCode, CancellationToken cancellationToken);
}

public sealed class ListKeyTypesUseCase : IListKeyTypesUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;

    public ListKeyTypesUseCase(IKeyCatalogPersistencePort catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public Task<IReadOnlyList<KeyTypeListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _catalog.ListKeyTypesAsync(cancellationToken);
    }
}

public sealed class ActivateKeyTypeUseCase : IActivateKeyTypeUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IOperatorAuditRecorder _audit;

    public ActivateKeyTypeUseCase(IKeyCatalogPersistencePort catalog, IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string typeCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeCode);
        KeyType? keyType = await _catalog.FindKeyTypeAsync(typeCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (keyType is null)
        {
            throw new InvalidOperationException("The key type was not found.");
        }

        keyType.Activate();
        _audit.Stage(
            OperatorAuditActions.KeyTypeActivated,
            OperatorAuditSubjects.KeyType,
            keyType.TypeCode);
        await _catalog.UpdateKeyTypeAsync(keyType, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class RetireKeyTypeUseCase : IRetireKeyTypeUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly IOperatorAuditRecorder _audit;

    public RetireKeyTypeUseCase(IKeyCatalogPersistencePort catalog, IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(string typeCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeCode);
        KeyType? keyType = await _catalog.FindKeyTypeAsync(typeCode.Trim(), cancellationToken).ConfigureAwait(false);
        if (keyType is null)
        {
            throw new InvalidOperationException("The key type was not found.");
        }

        int activeKeyAssets = await _catalog
            .CountActiveKeyAccessPatternsForTypeAsync(keyType.TypeCode, cancellationToken)
            .ConfigureAwait(false);
        keyType.Retire(hasActiveKeyAccessPatterns: activeKeyAssets > 0);
        _audit.Stage(
            OperatorAuditActions.KeyTypeRetired,
            OperatorAuditSubjects.KeyType,
            keyType.TypeCode);
        await _catalog.UpdateKeyTypeAsync(keyType, cancellationToken).ConfigureAwait(false);
    }
}
