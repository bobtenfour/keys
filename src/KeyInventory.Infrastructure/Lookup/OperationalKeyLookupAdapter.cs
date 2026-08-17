using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Lookup;

public sealed class OperationalKeyLookupAdapter : IOperationalKeyLookupPort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyAccessResolutionPort _accessResolution;

    public OperationalKeyLookupAdapter(
        KeyInventoryDbContext dbContext,
        IKeyAccessResolutionPort accessResolution)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _accessResolution = accessResolution ?? throw new ArgumentNullException(nameof(accessResolution));
    }

    public async Task<IReadOnlyList<KeyLookupResult>> SearchKeysAsync(
        string query,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        string pattern = query.Trim();

        var openLoans = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                where loan.Status == nameof(LoanStatus.Open)
                select new
                {
                    loan.KeyAssetId,
                    loan.LoanCode,
                    loan.BorrowerPartyReference
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, (string LoanCode, string PartyCode)> openByAsset = openLoans
            .GroupBy(loan => loan.KeyAssetId)
            .ToDictionary(
                group => group.Key,
                group => (group.First().LoanCode, group.First().BorrowerPartyReference));

        // Room reverse-search: Regular RoomCode match OR all Master KEY #s when RoomNumber matches.
        List<KeyAssetEntity> keys = await _dbContext.KeyAssets.AsNoTracking()
            .Include(key => key.AccessPattern)
            .Where(key =>
                key.KeyNumber.Contains(pattern)
                || key.MedecoKeyCode.Contains(pattern)
                || key.AccessPattern.Classification.Contains(pattern)
                || (key.AccessPattern.Classification == nameof(KeyAccessClassification.Regular)
                    && key.AccessPattern.RoomCode != null
                    && _dbContext.Rooms.Any(room =>
                        room.RoomCode == key.AccessPattern.RoomCode
                        && room.RoomNumber.Contains(pattern)))
                || (key.AccessPattern.Classification == nameof(KeyAccessClassification.Master)
                    && _dbContext.Rooms.Any(room => room.RoomNumber.Contains(pattern))))
            .OrderBy(key => key.KeyNumber)
            .ThenBy(key => key.MedecoKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> partyCodes = openByAsset.Values
            .Select(item => item.PartyCode)
            .ToHashSet(StringComparer.Ordinal);

        Dictionary<string, PartyEntity> parties = await _dbContext.Parties.AsNoTracking()
            .Where(party => partyCodes.Contains(party.PartyCode))
            .ToDictionaryAsync(party => party.PartyCode, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await _accessResolution.ResolveForPatternsAsync(
                    keys.GroupBy(key => key.KeyNumber, StringComparer.Ordinal)
                        .Select(group =>
                        {
                            KeyAssetEntity first = group.First();
                            return new KeyAccessResolutionRequest(
                                first.KeyNumber,
                                DomainCatalogMapper.ParseClassification(first.AccessPattern.Classification),
                                first.AccessPattern.RoomCode);
                        }),
                    expandMaster: false,
                    cancellationToken)
                .ConfigureAwait(false);

        List<KeyLookupResult> results = [];
        foreach (KeyAssetEntity key in keys)
        {
            IReadOnlyList<KeyOpenedRoomItem> openedRooms =
                roomsByKey.TryGetValue(key.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : [];

            KeyAccessClassification classification =
                DomainCatalogMapper.ParseClassification(key.AccessPattern.Classification);
            KeyPhysicalCondition condition = DomainCatalogMapper.ParseCondition(key.Condition);
            bool isIssued = openByAsset.ContainsKey(key.KeyAssetId);
            string custody = OperationalKeyAvailability.DeriveCustody(condition, isIssued);

            if (isIssued)
            {
                (string LoanCode, string PartyCode) open = openByAsset[key.KeyAssetId];
                parties.TryGetValue(open.PartyCode, out PartyEntity? party);
                PartyHolderDisplay? holder = party is null
                    ? null
                    : new PartyHolderDisplay(party.FirstName, party.LastName, party.Uin);
                results.Add(new KeyLookupResult(
                    key.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    classification,
                    condition,
                    custody,
                    holder,
                    open.LoanCode,
                    openedRooms));
            }
            else
            {
                results.Add(new KeyLookupResult(
                    key.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    classification,
                    condition,
                    custody,
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

        return await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                where loan.Status == nameof(LoanStatus.Open)
                    && loan.BorrowerPartyReference == member.PartyCode
                orderby key.KeyNumber, key.MedecoKeyCode
                select new IssuedKeyForMemberItem(
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
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
        var rows = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join pattern in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals pattern.KeyNumber
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Open)
                orderby loan.LoanCode
                select new
                {
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    Classification = pattern.Classification,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new OperationalLoanDisplay(
                row.LoanCode,
                row.KeyAssetId,
                row.KeyNumber,
                row.MedecoKeyCode,
                DomainCatalogMapper.ParseClassification(row.Classification),
                row.FirstName,
                row.LastName,
                row.Uin,
                row.IssuedAtUtc,
                row.DueAtUtc,
                row.Status,
                null))
            .ToArray();
    }

    public async Task<IReadOnlyList<OperationalLoanDisplay>> SearchOpenLoansWithHoldersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchText) || maxResults < 1)
        {
            return [];
        }

        string term = searchText.Trim();
        int bound = Math.Min(maxResults, IOperationalKeyLookupUseCase.DefaultOpenLoanSearchMaxResults);

        var rows = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join pattern in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals pattern.KeyNumber
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Open)
                    && (key.KeyNumber.Contains(term)
                        || key.MedecoKeyCode.Contains(term)
                        || party.FirstName.Contains(term)
                        || party.LastName.Contains(term)
                        || (party.FirstName + " " + party.LastName).Contains(term)
                        || party.Uin.Contains(term))
                orderby loan.LoanCode
                select new
                {
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    Classification = pattern.Classification,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status
                })
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new OperationalLoanDisplay(
                row.LoanCode,
                row.KeyAssetId,
                row.KeyNumber,
                row.MedecoKeyCode,
                DomainCatalogMapper.ParseClassification(row.Classification),
                row.FirstName,
                row.LastName,
                row.Uin,
                row.IssuedAtUtc,
                row.DueAtUtc,
                row.Status,
                null))
            .ToArray();
    }

    public async Task<OperationalLoanDisplay?> FindOpenLoanByLoanCodeAsync(
        string loanCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(loanCode))
        {
            return null;
        }

        var row = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join pattern in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals pattern.KeyNumber
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Open)
                    && loan.LoanCode == loanCode
                select new
                {
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    Classification = pattern.Classification,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status
                })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return row is null
            ? null
            : new OperationalLoanDisplay(
                row.LoanCode,
                row.KeyAssetId,
                row.KeyNumber,
                row.MedecoKeyCode,
                DomainCatalogMapper.ParseClassification(row.Classification),
                row.FirstName,
                row.LastName,
                row.Uin,
                row.IssuedAtUtc,
                row.DueAtUtc,
                row.Status,
                null);
    }

    public async Task<IReadOnlyList<OperationalLoanDisplay>> ListReturnedLoansWithHoldersAsync(
        CancellationToken cancellationToken)
    {
        var returnedRows = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join pattern in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals pattern.KeyNumber
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Returned)
                select new
                {
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    Classification = pattern.Classification,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status,
                    ReturnedAtUtc = (DateTimeOffset?)completedReturn.ReturnedAtUtc
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var closedWithoutReturn = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join pattern in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals pattern.KeyNumber
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Lost)
                    || loan.Status == nameof(LoanStatus.Destroyed)
                select new
                {
                    loan.LoanCode,
                    loan.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    Classification = pattern.Classification,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status,
                    ReturnedAtUtc = (DateTimeOffset?)null
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return returnedRows
            .Concat(closedWithoutReturn)
            .OrderByDescending(row => row.ReturnedAtUtc ?? row.IssuedAtUtc)
            .ThenBy(row => row.LoanCode, StringComparer.OrdinalIgnoreCase)
            .Select(row => new OperationalLoanDisplay(
                row.LoanCode,
                row.KeyAssetId,
                row.KeyNumber,
                row.MedecoKeyCode,
                DomainCatalogMapper.ParseClassification(row.Classification),
                row.FirstName,
                row.LastName,
                row.Uin,
                row.IssuedAtUtc,
                row.DueAtUtc,
                row.Status,
                row.ReturnedAtUtc))
            .ToArray();
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
