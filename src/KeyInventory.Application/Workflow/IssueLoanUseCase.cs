using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;

namespace KeyInventory.Application.Workflow;

public sealed class IssueLoanUseCase : IIssueLoanUseCase
{
    private readonly IKeyCatalogPersistencePort _catalog;
    private readonly ILoanPersistencePort _loans;

    public IssueLoanUseCase(IKeyCatalogPersistencePort catalog, ILoanPersistencePort loans)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _loans = loans ?? throw new ArgumentNullException(nameof(loans));
    }

    public async Task ExecuteAsync(
        string loanCode,
        string catalogKeyCode,
        string borrowerPartyReference,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset dueAtUtc,
        CancellationToken cancellationToken)
    {
        if (await _loans.LoanExistsAsync(loanCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A loan with this loan code already exists.");
        }

        KeyAsset? keyAsset = await _catalog.FindKeyAssetAsync(catalogKeyCode, cancellationToken).ConfigureAwait(false);
        if (keyAsset is null)
        {
            throw new InvalidOperationException("The selected key was not found.");
        }

        if (!keyAsset.IsActive)
        {
            throw new InvalidOperationException("An inactive key cannot be loaned.");
        }

        Loan loan = new(loanCode, keyAsset, borrowerPartyReference, issuedAtUtc, dueAtUtc);
        await _loans.AddLoanAsync(loan, cancellationToken).ConfigureAwait(false);
    }
}
