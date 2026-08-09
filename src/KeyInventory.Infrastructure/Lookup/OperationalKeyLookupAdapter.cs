using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Lookup;

public sealed class OperationalKeyLookupAdapter : IOperationalKeyLookupPort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyRoomAssignmentPersistencePort _roomAssignments;

    public OperationalKeyLookupAdapter(
        KeyInventoryDbContext dbContext,
        IKeyRoomAssignmentPersistencePort roomAssignments)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _roomAssignments = roomAssignments ?? throw new ArgumentNullException(nameof(roomAssignments));
    }

    public async Task<IReadOnlyList<KeyLookupResult>> SearchKeysAsync(
        string query,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        string pattern = query.Trim();

        var openLoans = await _dbContext.Loans.AsNoTracking()
            .Where(loan => loan.Status == nameof(LoanStatus.Open))
            .Select(loan => new
            {
                loan.CatalogKeyCode,
                loan.LoanCode,
                loan.BorrowerPartyReference
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, (string LoanCode, string PartyCode)> openByKey = openLoans
            .GroupBy(loan => loan.CatalogKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (group.First().LoanCode, group.First().BorrowerPartyReference),
                StringComparer.OrdinalIgnoreCase);

        List<KeyAssetEntity> keys = await _dbContext.KeyAssets.AsNoTracking()
            .Where(key =>
                key.CatalogKeyCode.Contains(pattern)
                || key.KeyTypeCode.Contains(pattern))
            .OrderBy(key => key.CatalogKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> partyCodes = openByKey.Values
            .Select(item => item.PartyCode)
            .ToHashSet(StringComparer.Ordinal);

        Dictionary<string, PartyEntity> parties = await _dbContext.Parties.AsNoTracking()
            .Where(party => partyCodes.Contains(party.PartyCode))
            .ToDictionaryAsync(party => party.PartyCode, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey = await _roomAssignments
            .ListForKeysAsync(keys.Select(key => key.CatalogKeyCode), cancellationToken)
            .ConfigureAwait(false);

        List<KeyLookupResult> results = [];
        foreach (KeyAssetEntity key in keys)
        {
            IReadOnlyList<KeyOpenedRoomItem> openedRooms =
                roomsByKey.TryGetValue(key.CatalogKeyCode, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : [];

            if (openByKey.TryGetValue(key.CatalogKeyCode, out (string LoanCode, string PartyCode) open))
            {
                parties.TryGetValue(open.PartyCode, out PartyEntity? party);
                PartyHolderDisplay? holder = party is null
                    ? null
                    : new PartyHolderDisplay(party.FirstName, party.LastName, party.Uin);
                results.Add(new KeyLookupResult(
                    key.CatalogKeyCode,
                    key.KeyTypeCode,
                    OperationalKeyAvailability.Issued,
                    holder,
                    open.LoanCode,
                    openedRooms));
            }
            else
            {
                results.Add(new KeyLookupResult(
                    key.CatalogKeyCode,
                    key.KeyTypeCode,
                    OperationalKeyAvailability.Available,
                    null,
                    null,
                    openedRooms));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<IssuedKeyForMemberItem>> ListIssuedKeysForWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workforceMemberCode);

        WorkforceMemberEntity? member = await _dbContext.WorkforceMembers.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.WorkforceMemberCode == workforceMemberCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            throw new InvalidOperationException("The workforce member was not found.");
        }

        PartyEntity? party = await _dbContext.Parties.AsNoTracking()
            .FirstOrDefaultAsync(item => item.PartyCode == member.PartyCode, cancellationToken)
            .ConfigureAwait(false);

        if (party is null)
        {
            throw new InvalidOperationException("The party for the workforce member was not found.");
        }

        return await _dbContext.Loans.AsNoTracking()
            .Where(loan =>
                loan.Status == nameof(LoanStatus.Open)
                && loan.BorrowerPartyReference == member.PartyCode)
            .OrderBy(loan => loan.CatalogKeyCode)
            .Select(loan => new IssuedKeyForMemberItem(
                loan.LoanCode,
                loan.CatalogKeyCode,
                party.FirstName,
                party.LastName,
                party.Uin,
                loan.IssuedAtUtc,
                loan.DueAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OperationalLoanDisplay>> ListOpenLoansWithHoldersAsync(
        CancellationToken cancellationToken)
    {
        return await (
                from loan in _dbContext.Loans.AsNoTracking()
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Open)
                orderby loan.LoanCode
                select new OperationalLoanDisplay(
                    loan.LoanCode,
                    loan.CatalogKeyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status,
                    null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OperationalLoanDisplay>> ListReturnedLoansWithHoldersAsync(
        CancellationToken cancellationToken)
    {
        return await (
                from loan in _dbContext.Loans.AsNoTracking()
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Returned)
                orderby loan.LoanCode
                select new OperationalLoanDisplay(
                    loan.LoanCode,
                    loan.CatalogKeyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status,
                    completedReturn.ReturnedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkforceMemberIdentityDisplay>> ListActiveWorkforceMembersWithIdentityAsync(
        CancellationToken cancellationToken)
    {
        return await (
                from member in _dbContext.WorkforceMembers.AsNoTracking()
                join party in _dbContext.Parties.AsNoTracking()
                    on member.PartyCode equals party.PartyCode
                where member.Status == nameof(WorkforceMemberStatus.Active)
                orderby member.WorkforceMemberCode
                select new WorkforceMemberIdentityDisplay(
                    member.WorkforceMemberCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    member.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
