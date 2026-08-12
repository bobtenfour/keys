using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Reports;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Reports;

public sealed class OperationalReportsAdapter : IOperationalReportsPort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyAccessPatternRoomAssignmentPersistencePort _roomAssignments;

    public OperationalReportsAdapter(
        KeyInventoryDbContext dbContext,
        IKeyAccessPatternRoomAssignmentPersistencePort roomAssignments)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _roomAssignments = roomAssignments ?? throw new ArgumentNullException(nameof(roomAssignments));
    }

    public async Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? keyNumberFilter,
        CancellationToken cancellationToken)
    {
        List<OpenLoanPartyRow> openLoans = await LoadOpenLoansAsync(keyNumberFilter, cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, WorkforceMemberEntity> membersByParty =
            await ResolveMembersByPartyAsync(openLoans.Select(loan => loan.PartyCode), cancellationToken)
                .ConfigureAwait(false);

        return openLoans
            .Select(loan =>
            {
                membersByParty.TryGetValue(loan.PartyCode, out WorkforceMemberEntity? member);
                return new CurrentKeyHolderReportRow(
                    loan.KeyNumber,
                    loan.MedecoKeyCode,
                    loan.FirstName,
                    loan.LastName,
                    loan.Uin,
                    member?.WorkforceMemberCode,
                    member?.DepartmentCode,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status);
            })
            .OrderBy(row => row.KeyNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.MedecoKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? keyNumberFilter,
        CancellationToken cancellationToken)
    {
        List<OpenLoanPartyRow> openLoans = await LoadOpenLoansAsync(keyNumberFilter, cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, WorkforceMemberEntity> membersByParty =
            await ResolveMembersByPartyAsync(openLoans.Select(loan => loan.PartyCode), cancellationToken)
                .ConfigureAwait(false);

        return openLoans
            .Select(loan =>
            {
                membersByParty.TryGetValue(loan.PartyCode, out WorkforceMemberEntity? member);
                return new ActiveLoanReportRow(
                    loan.KeyNumber,
                    loan.MedecoKeyCode,
                    loan.FirstName,
                    loan.LastName,
                    loan.Uin,
                    member?.WorkforceMemberCode,
                    member?.DepartmentCode,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status);
            })
            .OrderBy(row => row.KeyNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.MedecoKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<OverdueKeyReportRow>> ListOverdueKeysAsync(
        DateTimeOffset utcNow,
        string? keyNumberFilter,
        CancellationToken cancellationToken)
    {
        List<OpenLoanPartyRow> openLoans = await LoadOpenLoansAsync(keyNumberFilter, cancellationToken)
            .ConfigureAwait(false);
        List<OpenLoanPartyRow> overdue = openLoans.Where(loan => loan.DueAtUtc < utcNow).ToList();
        Dictionary<string, WorkforceMemberEntity> membersByParty =
            await ResolveMembersByPartyAsync(overdue.Select(loan => loan.PartyCode), cancellationToken)
                .ConfigureAwait(false);

        return overdue
            .Select(loan =>
            {
                membersByParty.TryGetValue(loan.PartyCode, out WorkforceMemberEntity? member);
                int daysOverdue = Math.Max(0, (int)Math.Floor((utcNow - loan.DueAtUtc).TotalDays));
                return new OverdueKeyReportRow(
                    loan.KeyNumber,
                    loan.MedecoKeyCode,
                    loan.FirstName,
                    loan.LastName,
                    loan.Uin,
                    member?.WorkforceMemberCode,
                    member?.DepartmentCode,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    daysOverdue,
                    loan.Status);
            })
            .OrderByDescending(row => row.DaysOverdue)
            .ThenBy(row => row.KeyNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.MedecoKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<KeysByWorkforceMemberReport?> GetKeysByWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        WorkforceMemberEntity? member = await _dbContext.WorkforceMembers.AsNoTracking()
            .FirstOrDefaultAsync(item => item.WorkforceMemberCode == workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (member is null)
        {
            return null;
        }

        PartyEntity? party = await _dbContext.Parties.AsNoTracking()
            .FirstOrDefaultAsync(item => item.PartyCode == member.PartyCode, cancellationToken)
            .ConfigureAwait(false);
        if (party is null)
        {
            throw new InvalidOperationException("The party for the workforce member was not found.");
        }

        List<MemberIssuedKeyReportRow> issued = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                where loan.Status == nameof(LoanStatus.Open)
                    && loan.BorrowerPartyReference == member.PartyCode
                orderby key.KeyNumber, key.MedecoKeyCode
                select new MemberIssuedKeyReportRow(
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<MemberReturnedKeyReportRow> returned = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                where loan.Status == nameof(LoanStatus.Returned)
                    && loan.BorrowerPartyReference == member.PartyCode
                orderby completedReturn.ReturnedAtUtc descending
                select new MemberReturnedKeyReportRow(
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    completedReturn.ReturnedAtUtc,
                    loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new KeysByWorkforceMemberReport(member.WorkforceMemberCode, issued, returned);
    }

    public async Task<IReadOnlyList<KeyHistoryReportRow>> ListKeyHistoryAsync(
        string keyNumber,
        CancellationToken cancellationToken)
    {
        bool patternExists = await _dbContext.KeyAccessPatterns.AsNoTracking()
            .AnyAsync(pattern => pattern.KeyNumber == keyNumber, cancellationToken)
            .ConfigureAwait(false);
        if (!patternExists)
        {
            return [];
        }

        var openRows = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where key.KeyNumber == keyNumber && loan.Status == nameof(LoanStatus.Open)
                select new KeyHistoryReportRow(
                    loan.LoanCode,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    null,
                    loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var returnedRows = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where key.KeyNumber == keyNumber && loan.Status == nameof(LoanStatus.Returned)
                select new KeyHistoryReportRow(
                    loan.LoanCode,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    completedReturn.ReturnedAtUtc,
                    loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return openRows
            .Concat(returnedRows)
            .OrderByDescending(row => row.IssuedAtUtc)
            .ThenBy(row => row.LoanCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<OutstandingWorkforceKeyReportRow>> ListOutstandingKeysByWorkforceStatusAsync(
        string? workforceStatusFilter,
        CancellationToken cancellationToken)
    {
        IQueryable<WorkforceMemberEntity> membersQuery = _dbContext.WorkforceMembers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(workforceStatusFilter))
        {
            membersQuery = membersQuery.Where(member => member.Status == workforceStatusFilter);
        }

        List<OutstandingWorkforceKeyReportRow> rows = await (
                from member in membersQuery
                join party in _dbContext.Parties.AsNoTracking()
                    on member.PartyCode equals party.PartyCode
                join loan in _dbContext.Loans.AsNoTracking()
                    on member.PartyCode equals loan.BorrowerPartyReference
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                where loan.Status == nameof(LoanStatus.Open)
                orderby member.Status, member.WorkforceMemberCode, key.KeyNumber, key.MedecoKeyCode
                select new OutstandingWorkforceKeyReportRow(
                    member.WorkforceMemberCode,
                    member.Status,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    member.DepartmentCode,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    loan.LoanCode,
                    loan.DueAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }

    public async Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? keyNumberFilter,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> issuedAssets = (await _dbContext.Loans.AsNoTracking()
                .Where(loan => loan.Status == nameof(LoanStatus.Open))
                .Select(loan => loan.KeyAssetId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToHashSet();

        IQueryable<KeyAssetEntity> keysQuery = _dbContext.KeyAssets.AsNoTracking()
            .Include(key => key.AccessPattern);
        if (!string.IsNullOrWhiteSpace(keyNumberFilter))
        {
            keysQuery = keysQuery.Where(key =>
                key.KeyNumber.Contains(keyNumberFilter)
                || key.MedecoKeyCode.Contains(keyNumberFilter));
        }

        List<KeyAssetEntity> keys = await keysQuery
            .OrderBy(key => key.KeyNumber)
            .ThenBy(key => key.MedecoKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey = await _roomAssignments
            .ListForKeyNumbersAsync(keys.Select(key => key.KeyNumber).Distinct(StringComparer.Ordinal), cancellationToken)
            .ConfigureAwait(false);

        return keys
            .Select(key => new KeyCatalogReportRow(
                key.KeyNumber,
                key.MedecoKeyCode,
                key.AccessPattern.KeyTypeCode,
                key.IsActive,
                issuedAssets.Contains(key.KeyAssetId)
                    ? OperationalKeyAvailability.Issued
                    : OperationalKeyAvailability.Available,
                roomsByKey.TryGetValue(key.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                    ? rooms
                    : []))
            .ToArray();
    }

    public async Task<IReadOnlyList<WorkforceMemberReportOption>> ListWorkforceMemberOptionsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        IQueryable<WorkforceMemberEntity> members = _dbContext.WorkforceMembers.AsNoTracking();
        IQueryable<PartyEntity> parties = _dbContext.Parties.AsNoTracking();

        var query =
            from member in members
            join party in parties on member.PartyCode equals party.PartyCode
            select new { member, party };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.member.WorkforceMemberCode.Contains(search)
                || item.party.FirstName.Contains(search)
                || item.party.LastName.Contains(search)
                || item.party.Uin.Contains(search));
        }

        return await query
            .OrderBy(item => item.member.WorkforceMemberCode)
            .Select(item => new WorkforceMemberReportOption(
                item.member.WorkforceMemberCode,
                item.party.FirstName,
                item.party.LastName,
                item.party.Uin,
                item.member.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListKeyNumbersAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        IQueryable<KeyAccessPatternEntity> patterns = _dbContext.KeyAccessPatterns.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            patterns = patterns.Where(pattern => pattern.KeyNumber.Contains(search));
        }

        return await patterns
            .OrderBy(pattern => pattern.KeyNumber)
            .Select(pattern => pattern.KeyNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<OpenLoanPartyRow>> LoadOpenLoansAsync(
        string? keyNumberFilter,
        CancellationToken cancellationToken)
    {
        var query =
            from loan in _dbContext.Loans.AsNoTracking()
            join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
            join party in _dbContext.Parties.AsNoTracking()
                on loan.BorrowerPartyReference equals party.PartyCode
            where loan.Status == nameof(LoanStatus.Open)
            select new { loan, key, party };

        if (!string.IsNullOrWhiteSpace(keyNumberFilter))
        {
            query = query.Where(item =>
                item.key.KeyNumber.Contains(keyNumberFilter)
                || item.key.MedecoKeyCode.Contains(keyNumberFilter));
        }

        return await query
            .OrderBy(item => item.key.KeyNumber)
            .ThenBy(item => item.key.MedecoKeyCode)
            .Select(item => new OpenLoanPartyRow(
                item.loan.LoanCode,
                item.key.KeyNumber,
                item.key.MedecoKeyCode,
                item.loan.BorrowerPartyReference,
                item.party.FirstName,
                item.party.LastName,
                item.party.Uin,
                item.loan.IssuedAtUtc,
                item.loan.DueAtUtc,
                item.loan.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Dictionary<string, WorkforceMemberEntity>> ResolveMembersByPartyAsync(
        IEnumerable<string> partyCodes,
        CancellationToken cancellationToken)
    {
        HashSet<string> codes = partyCodes.ToHashSet(StringComparer.Ordinal);
        if (codes.Count == 0)
        {
            return new Dictionary<string, WorkforceMemberEntity>(StringComparer.Ordinal);
        }

        List<WorkforceMemberEntity> members = await _dbContext.WorkforceMembers.AsNoTracking()
            .Where(member => codes.Contains(member.PartyCode))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return members
            .GroupBy(member => member.PartyCode, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(member =>
                        string.Equals(member.Status, nameof(WorkforceMemberStatus.Active), StringComparison.Ordinal))
                    .ThenBy(member => member.WorkforceMemberCode, StringComparer.OrdinalIgnoreCase)
                    .First(),
                StringComparer.Ordinal);
    }

    private sealed record OpenLoanPartyRow(
        string LoanCode,
        string KeyNumber,
        string MedecoKeyCode,
        string PartyCode,
        string FirstName,
        string LastName,
        string Uin,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset DueAtUtc,
        string Status);
}
