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
            .ThenInclude(asset => asset.AccessPattern)
            .ThenInclude(pattern => pattern.KeyType)
            .FirstOrDefaultAsync(
                item => item.LoanCode == loanCode && item.Status == nameof(LoanStatus.Open),
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        List<string> roomCodes = await _dbContext.KeyAccessPatternRoomAssignments.AsNoTracking()
            .Where(item => item.KeyNumber == entity.KeyAsset.KeyNumber)
            .Select(item => item.RoomCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return DomainLoanMapper.ToOpenDomainLoan(entity, roomCodes);
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
        List<LoanListItem> items = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode into partyJoin
                from party in partyJoin.DefaultIfEmpty()
                where loan.Status == nameof(LoanStatus.Open)
                orderby loan.LoanCode
                select new LoanListItem(
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    loan.BorrowerPartyReference,
                    party != null ? party.FirstName : null,
                    party != null ? party.LastName : null,
                    party != null ? party.Uin : null,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }

    public async Task<IReadOnlyList<LoanListItem>> ListReturnedLoansAsync(CancellationToken cancellationToken)
    {
        List<LoanListItem> items = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode into partyJoin
                from party in partyJoin.DefaultIfEmpty()
                where loan.Status == nameof(LoanStatus.Returned)
                orderby loan.LoanCode
                select new LoanListItem(
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    loan.BorrowerPartyReference,
                    party != null ? party.FirstName : null,
                    party != null ? party.LastName : null,
                    party != null ? party.Uin : null,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items;
    }

    public Task<bool> HasOpenLoanForKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        return _dbContext.Loans.AnyAsync(
            entity => entity.KeyAssetId == keyAssetId && entity.Status == nameof(LoanStatus.Open),
            cancellationToken);
    }

    public Task<int> CountLoansForKeyAssetAsync(Guid keyAssetId, CancellationToken cancellationToken)
    {
        return _dbContext.Loans.CountAsync(
            entity => entity.KeyAssetId == keyAssetId,
            cancellationToken);
    }

    public Task<int> CountLoansForPartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        return _dbContext.Loans.CountAsync(
            entity => entity.BorrowerPartyReference == partyCode,
            cancellationToken);
    }

    public Task<int> CountLoansJustifiedByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Loans.CountAsync(
            entity => entity.JustificationDepartmentId == departmentId,
            cancellationToken);
    }

    public Task<int> CountLoansJustifiedByRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        return _dbContext.Loans.CountAsync(
            entity => entity.JustificationRoomCode == roomCode,
            cancellationToken);
    }
}
