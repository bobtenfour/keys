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

    public OperationalReportsAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<CurrentKeyHolderReportRow>> ListCurrentKeyHoldersAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        List<OpenLoanPartyRow> openLoans = await LoadOpenLoansAsync(catalogKeyCodeFilter, cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, WorkforceMemberEntity> membersByParty =
            await ResolveMembersByPartyAsync(openLoans.Select(loan => loan.PartyCode), cancellationToken)
                .ConfigureAwait(false);

        return openLoans
            .Select(loan =>
            {
                membersByParty.TryGetValue(loan.PartyCode, out WorkforceMemberEntity? member);
                return new CurrentKeyHolderReportRow(
                    loan.CatalogKeyCode,
                    loan.FirstName,
                    loan.LastName,
                    loan.Uin,
                    member?.WorkforceMemberCode,
                    member?.DepartmentCode,
                    member?.ResponsibleManagerWorkforceMemberCode,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status);
            })
            .OrderBy(row => row.CatalogKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<ActiveLoanReportRow>> ListActiveLoansReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        List<OpenLoanPartyRow> openLoans = await LoadOpenLoansAsync(catalogKeyCodeFilter, cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, WorkforceMemberEntity> membersByParty =
            await ResolveMembersByPartyAsync(openLoans.Select(loan => loan.PartyCode), cancellationToken)
                .ConfigureAwait(false);

        return openLoans
            .Select(loan =>
            {
                membersByParty.TryGetValue(loan.PartyCode, out WorkforceMemberEntity? member);
                return new ActiveLoanReportRow(
                    loan.CatalogKeyCode,
                    loan.FirstName,
                    loan.LastName,
                    loan.Uin,
                    member?.WorkforceMemberCode,
                    member?.DepartmentCode,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status);
            })
            .OrderBy(row => row.CatalogKeyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<OverdueKeyReportRow>> ListOverdueKeysAsync(
        DateTimeOffset utcNow,
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        List<OpenLoanPartyRow> openLoans = await LoadOpenLoansAsync(catalogKeyCodeFilter, cancellationToken)
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
                    loan.CatalogKeyCode,
                    loan.FirstName,
                    loan.LastName,
                    loan.Uin,
                    member?.WorkforceMemberCode,
                    member?.ResponsibleManagerWorkforceMemberCode,
                    member?.DepartmentCode,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    daysOverdue,
                    loan.Status);
            })
            .OrderByDescending(row => row.DaysOverdue)
            .ThenBy(row => row.CatalogKeyCode, StringComparer.OrdinalIgnoreCase)
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

        List<MemberIssuedKeyReportRow> issued = await _dbContext.Loans.AsNoTracking()
            .Where(loan =>
                loan.Status == nameof(LoanStatus.Open)
                && loan.BorrowerPartyReference == member.PartyCode)
            .OrderBy(loan => loan.CatalogKeyCode)
            .Select(loan => new MemberIssuedKeyReportRow(
                loan.CatalogKeyCode,
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
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                where loan.Status == nameof(LoanStatus.Returned)
                    && loan.BorrowerPartyReference == member.PartyCode
                orderby completedReturn.ReturnedAtUtc descending
                select new MemberReturnedKeyReportRow(
                    loan.CatalogKeyCode,
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
        string catalogKeyCode,
        CancellationToken cancellationToken)
    {
        bool keyExists = await _dbContext.KeyAssets.AsNoTracking()
            .AnyAsync(key => key.CatalogKeyCode == catalogKeyCode, cancellationToken)
            .ConfigureAwait(false);
        if (!keyExists)
        {
            return [];
        }

        var openRows = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.CatalogKeyCode == catalogKeyCode && loan.Status == nameof(LoanStatus.Open)
                select new KeyHistoryReportRow(
                    loan.LoanCode,
                    loan.CatalogKeyCode,
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
                join completedReturn in _dbContext.Returns.AsNoTracking()
                    on loan.LoanCode equals completedReturn.LoanCode
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.CatalogKeyCode == catalogKeyCode && loan.Status == nameof(LoanStatus.Returned)
                select new KeyHistoryReportRow(
                    loan.LoanCode,
                    loan.CatalogKeyCode,
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
                where loan.Status == nameof(LoanStatus.Open)
                orderby member.Status, member.WorkforceMemberCode, loan.CatalogKeyCode
                select new OutstandingWorkforceKeyReportRow(
                    member.WorkforceMemberCode,
                    member.Status,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    member.DepartmentCode,
                    member.ResponsibleManagerWorkforceMemberCode,
                    loan.CatalogKeyCode,
                    loan.LoanCode,
                    loan.DueAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }

    public async Task<IReadOnlyList<KeyCatalogReportRow>> ListKeyCatalogReportAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        HashSet<string> issuedKeys = (await _dbContext.Loans.AsNoTracking()
                .Where(loan => loan.Status == nameof(LoanStatus.Open))
                .Select(loan => loan.CatalogKeyCode)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IQueryable<KeyAssetEntity> keysQuery = _dbContext.KeyAssets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(catalogKeyCodeFilter))
        {
            keysQuery = keysQuery.Where(key => key.CatalogKeyCode.Contains(catalogKeyCodeFilter));
        }

        List<KeyAssetEntity> keys = await keysQuery
            .OrderBy(key => key.CatalogKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return keys
            .Select(key => new KeyCatalogReportRow(
                key.CatalogKeyCode,
                key.KeyTypeCode,
                key.IsActive,
                issuedKeys.Contains(key.CatalogKeyCode)
                    ? OperationalKeyAvailability.Issued
                    : OperationalKeyAvailability.Available))
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

    public async Task<IReadOnlyList<string>> ListCatalogKeyCodesAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        IQueryable<KeyAssetEntity> keys = _dbContext.KeyAssets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            keys = keys.Where(key => key.CatalogKeyCode.Contains(search));
        }

        return await keys
            .OrderBy(key => key.CatalogKeyCode)
            .Select(key => key.CatalogKeyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<OpenLoanPartyRow>> LoadOpenLoansAsync(
        string? catalogKeyCodeFilter,
        CancellationToken cancellationToken)
    {
        IQueryable<LoanEntity> loans = _dbContext.Loans.AsNoTracking()
            .Where(loan => loan.Status == nameof(LoanStatus.Open));
        if (!string.IsNullOrWhiteSpace(catalogKeyCodeFilter))
        {
            loans = loans.Where(loan => loan.CatalogKeyCode.Contains(catalogKeyCodeFilter));
        }

        return await (
                from loan in loans
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                orderby loan.CatalogKeyCode
                select new OpenLoanPartyRow(
                    loan.LoanCode,
                    loan.CatalogKeyCode,
                    loan.BorrowerPartyReference,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    loan.IssuedAtUtc,
                    loan.DueAtUtc,
                    loan.Status))
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
        string CatalogKeyCode,
        string PartyCode,
        string FirstName,
        string LastName,
        string Uin,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset DueAtUtc,
        string Status);
}
