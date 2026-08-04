using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Loans;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Workflow;

public sealed class LoanPersistenceAdapter : ILoanPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;

    public LoanPersistenceAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<bool> LoanExistsAsync(string loanCode, CancellationToken cancellationToken)
    {
        return _dbContext.Loans.AnyAsync(entity => entity.LoanCode == loanCode, cancellationToken);
    }

    public async Task AddLoanAsync(Loan loan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loan);
        _dbContext.Loans.Add(DomainLoanMapper.ToEntity(loan));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Loan?> FindOpenLoanAsync(string loanCode, CancellationToken cancellationToken)
    {
        LoanEntity? entity = await _dbContext.Loans
            .AsNoTracking()
            .Include(item => item.KeyAsset)
            .ThenInclude(asset => asset.KeyType)
            .FirstOrDefaultAsync(
                item => item.LoanCode == loanCode && item.Status == nameof(LoanStatus.Open),
                cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : DomainLoanMapper.ToOpenDomainLoan(entity);
    }

    public async Task AddReturnAsync(Return completedReturn, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(completedReturn);

        LoanEntity? loanEntity = await _dbContext.Loans
            .FirstOrDefaultAsync(item => item.LoanCode == completedReturn.Loan.LoanCode, cancellationToken)
            .ConfigureAwait(false);

        if (loanEntity is null)
        {
            throw new InvalidOperationException("The loan to return was not found in persistence.");
        }

        loanEntity.Status = completedReturn.Loan.Status.ToString();
        _dbContext.Returns.Add(DomainLoanMapper.ToEntity(completedReturn));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LoanListItem>> ListOpenLoansAsync(CancellationToken cancellationToken)
    {
        List<LoanListItem> items = await _dbContext.Loans
            .AsNoTracking()
            .Where(entity => entity.Status == nameof(LoanStatus.Open))
            .OrderBy(entity => entity.LoanCode)
            .Select(entity => new LoanListItem(
                entity.LoanCode,
                entity.CatalogKeyCode,
                entity.BorrowerPartyReference,
                entity.IssuedAtUtc,
                entity.DueAtUtc,
                entity.Status,
                null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }

    public async Task<IReadOnlyList<LoanListItem>> ListReturnedLoansAsync(CancellationToken cancellationToken)
    {
        List<LoanListItem> items = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                where loan.Status == nameof(LoanStatus.Returned)
                orderby loan.LoanCode
                select new LoanListItem(
                    loan.LoanCode,
                    loan.CatalogKeyCode,
                    loan.BorrowerPartyReference,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status,
                    completedReturn.ReturnedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }
}
