using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Lifecycle;

public interface IMarkKeyAssetLostUseCase
{
    Task ExecuteAsync(Guid keyAssetId, CancellationToken cancellationToken);
}

public interface IDestroyKeyAssetUseCase
{
    Task ExecuteAsync(Guid keyAssetId, CancellationToken cancellationToken);
}

public interface IReplaceLostKeyUseCase
{
    Task<Guid> ExecuteAsync(
        Guid lostKeyAssetId,
        string newMedecoKeyCode,
        CancellationToken cancellationToken);
}

public sealed class MarkKeyAssetLostUseCase : IMarkKeyAssetLostUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly ILoanPersistencePort _loans;
    private readonly IOperatorAuditRecorder _audit;

    public MarkKeyAssetLostUseCase(
        IKeyCatalogPersistencePort catalog,
        ILoanPersistencePort loans,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The key was not found.");
        }

        if (keyAsset.Condition != KeyPhysicalCondition.Active)
        {
            throw new InvalidOperationException("Only an Active key may be marked Lost.");
        }

        Loan? openLoan = await _loans.FindOpenLoanForKeyAssetAsync(keyAsset.KeyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (openLoan is not null)
        {
            openLoan.CloseAsLost();
            _audit.Stage(
                OperatorAuditActions.CustodyClosedLost,
                OperatorAuditSubjects.Loan,
                openLoan.LoanCode,
                $"KEY#={keyAsset.KeyNumber}; MEDECO={keyAsset.MedecoKeyCode}; KeyAssetId={keyAsset.KeyAssetId:D}");
            await _loans.UpdateLoanAsync(openLoan, cancellationToken).ConfigureAwait(false);
        }

        keyAsset.MarkLost();
        _audit.Stage(
            OperatorAuditActions.KeyMarkedLost,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}");
        await _catalog.UpdateKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DestroyKeyAssetUseCase : IDestroyKeyAssetUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly ILoanPersistencePort _loans;
    private readonly IOperatorAuditRecorder _audit;

    public DestroyKeyAssetUseCase(
        IKeyCatalogPersistencePort catalog,
        ILoanPersistencePort loans,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task ExecuteAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        if (keyAssetId == Guid.Empty)
        {
            throw new ArgumentException("KeyAssetId is required.", nameof(keyAssetId));
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(keyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The key was not found.");
        }

        if (keyAsset.Condition == KeyPhysicalCondition.Destroyed)
        {
            throw new InvalidOperationException("The key is already Destroyed.");
        }

        Loan? openLoan = await _loans.FindOpenLoanForKeyAssetAsync(keyAsset.KeyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (openLoan is not null)
        {
            if (keyAsset.Condition != KeyPhysicalCondition.Active)
            {
                throw new InvalidOperationException(
                    "A Lost key must not have an open Loan. Resolve custody before Destroy.");
            }

            openLoan.CloseAsDestroyed();
            _audit.Stage(
                OperatorAuditActions.CustodyClosedDestroyed,
                OperatorAuditSubjects.Loan,
                openLoan.LoanCode,
                $"KEY#={keyAsset.KeyNumber}; MEDECO={keyAsset.MedecoKeyCode}; KeyAssetId={keyAsset.KeyAssetId:D}");
            await _loans.UpdateLoanAsync(openLoan, cancellationToken).ConfigureAwait(false);
        }

        keyAsset.Destroy();
        _audit.Stage(
            OperatorAuditActions.KeyDestroyed,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{keyAsset.KeyNumber}/{keyAsset.MedecoKeyCode}",
            $"KeyAssetId={keyAsset.KeyAssetId:D}");
        await _catalog.UpdateKeyAssetAsync(keyAsset, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ReplaceLostKeyUseCase : IReplaceLostKeyUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly ILoanPersistencePort _loans;
    private readonly IOperatorAuditRecorder _audit;

    public ReplaceLostKeyUseCase(
        IKeyCatalogPersistencePort catalog,
        ILoanPersistencePort loans,
        IOperatorAuditRecorder audit)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<Guid> ExecuteAsync(
        Guid lostKeyAssetId,
        string newMedecoKeyCode,
        CancellationToken cancellationToken)
    {
        if (lostKeyAssetId == Guid.Empty)
        {
            throw new ArgumentException("Lost KeyAssetId is required.", nameof(lostKeyAssetId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newMedecoKeyCode);
        string normalizedMedeco = newMedecoKeyCode.Trim();

        KeyAsset? source = await _catalog.FindKeyAssetAsync(lostKeyAssetId, cancellationToken)
            .ConfigureAwait(false);
        if (source is null)
        {
            throw new InvalidOperationException("The lost key was not found.");
        }

        if (source.Condition != KeyPhysicalCondition.Lost)
        {
            throw new InvalidOperationException("Only a Lost key may be replaced.");
        }

        if (await _loans.HasOpenLoanForKeyAssetAsync(source.KeyAssetId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A Lost key must not have an open Loan.");
        }

        KeyAccessPattern? pattern = await _catalog.FindKeyAccessPatternAsync(source.KeyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (pattern is null || !pattern.IsActive)
        {
            throw new InvalidOperationException("The parent KEY # must be active to replace a lost key.");
        }

        if (await _catalog.MedecoExistsUnderPatternAsync(pattern.KeyNumber, normalizedMedeco, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException("That MEDECO already exists under this KEY #.");
        }

        KeyAsset replacement = new(Guid.NewGuid(), pattern, normalizedMedeco, source.KeyAssetId);
        _audit.Stage(
            OperatorAuditActions.LostKeyReplaced,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{source.KeyNumber}/{source.MedecoKeyCode}",
            $"SourceKeyAssetId={source.KeyAssetId:D}; ReplacementKeyAssetId={replacement.KeyAssetId:D}; NewMEDECO={replacement.MedecoKeyCode}");
        _audit.Stage(
            OperatorAuditActions.PhysicalKeyCopyRegistered,
            OperatorAuditSubjects.PhysicalKeyCopy,
            $"{replacement.KeyNumber}/{replacement.MedecoKeyCode}",
            $"KeyAssetId={replacement.KeyAssetId:D}; Replaces={source.KeyAssetId:D}");
        await _catalog.AddKeyAssetAsync(replacement, cancellationToken).ConfigureAwait(false);
        return replacement.KeyAssetId;
    }
}
