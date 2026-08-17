using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Loans;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Lookup;

/// <summary>
/// Composes global operator search from existing SQL authorities. No search-index table.
/// </summary>
public sealed class GlobalOperatorSearchAdapter : IGlobalOperatorSearchPort
{
    private readonly KeyInventoryDbContext _dbContext;
    private readonly IKeyAccessResolutionPort _accessResolution;

    public GlobalOperatorSearchAdapter(
        KeyInventoryDbContext dbContext,
        IKeyAccessResolutionPort accessResolution)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _accessResolution = accessResolution ?? throw new ArgumentNullException(nameof(accessResolution));
    }

    public async Task<GlobalOperatorSearchResult> SearchAsync(
        string query,
        int maxPerCategory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPerCategory, 1);

        string pattern = query.Trim();
        int bound = maxPerCategory;

        IReadOnlyList<GlobalPersonSearchHit> people = await SearchPeopleAsync(pattern, bound, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<GlobalRoomSearchHit> rooms = await SearchRoomsAsync(pattern, bound, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<GlobalKeyNumberSearchHit> keyNumbers = await SearchKeyNumbersAsync(pattern, bound, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<GlobalMedecoSearchHit> medecos = await SearchMedecoCopiesAsync(pattern, bound, cancellationToken)
            .ConfigureAwait(false);

        return new GlobalOperatorSearchResult(pattern, people, rooms, keyNumbers, medecos);
    }

    private async Task<IReadOnlyList<GlobalPersonSearchHit>> SearchPeopleAsync(
        string pattern,
        int bound,
        CancellationToken cancellationToken)
    {
        var memberRows = await (
                from member in _dbContext.WorkforceMembers.AsNoTracking()
                join party in _dbContext.Parties.AsNoTracking() on member.PartyCode equals party.PartyCode
                join department in _dbContext.Departments.AsNoTracking()
                    on member.DepartmentId equals department.DepartmentId
                where party.FirstName.Contains(pattern)
                    || party.LastName.Contains(pattern)
                    || (party.FirstName + " " + party.LastName).Contains(pattern)
                    || party.Uin.Contains(pattern)
                orderby party.LastName, party.FirstName, party.Uin
                select new
                {
                    member.WorkforceMemberCode,
                    member.PartyCode,
                    member.Status,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    department.DepartmentCode
                })
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (memberRows.Count == 0)
        {
            return [];
        }

        HashSet<string> memberCodes = memberRows
            .Select(row => row.WorkforceMemberCode)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> partyCodes = memberRows
            .Select(row => row.PartyCode)
            .ToHashSet(StringComparer.Ordinal);

        var assignments = await (
                from assignment in _dbContext.WorkAssignments.AsNoTracking()
                join room in _dbContext.Rooms.AsNoTracking() on assignment.RoomCode equals room.RoomCode
                where assignment.IsActive
                    && memberCodes.Contains(assignment.WorkforceMemberCode)
                orderby room.RoomNumber
                select new
                {
                    assignment.WorkforceMemberCode,
                    room.RoomNumber
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, List<GlobalPersonWorkAssignment>> assignmentsByMember = assignments
            .GroupBy(item => item.WorkforceMemberCode, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new GlobalPersonWorkAssignment(item.RoomNumber))
                    .ToList(),
                StringComparer.Ordinal);

        var openCustody = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join key in _dbContext.KeyAssets.AsNoTracking() on loan.KeyAssetId equals key.KeyAssetId
                join accessPattern in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals accessPattern.KeyNumber
                where loan.Status == nameof(LoanStatus.Open)
                    && partyCodes.Contains(loan.BorrowerPartyReference)
                orderby key.KeyNumber, key.MedecoKeyCode
                select new
                {
                    loan.BorrowerPartyReference,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    accessPattern.Classification,
                    loan.IssuedAtUtc
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await ResolveRoomsForKeyNumbersAsync(
                    openCustody.Select(item => item.KeyNumber).Distinct(StringComparer.Ordinal),
                    cancellationToken)
                .ConfigureAwait(false);

        Dictionary<string, List<GlobalPersonCurrentKey>> keysByParty = openCustody
            .GroupBy(item => item.BorrowerPartyReference, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new GlobalPersonCurrentKey(
                        item.KeyNumber,
                        item.MedecoKeyCode,
                        DomainCatalogMapper.ParseClassification(item.Classification),
                        item.IssuedAtUtc,
                        roomsByKey.TryGetValue(item.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? rooms)
                            ? rooms
                            : []))
                    .ToList(),
                StringComparer.Ordinal);

        return memberRows
            .Select(row => new GlobalPersonSearchHit(
                row.WorkforceMemberCode,
                row.FirstName,
                row.LastName,
                row.Uin,
                row.DepartmentCode,
                row.Status,
                assignmentsByMember.TryGetValue(row.WorkforceMemberCode, out List<GlobalPersonWorkAssignment>? work)
                    ? work
                    : [],
                keysByParty.TryGetValue(row.PartyCode, out List<GlobalPersonCurrentKey>? keys)
                    ? keys
                    : []))
            .ToArray();
    }

    private async Task<IReadOnlyList<GlobalRoomSearchHit>> SearchRoomsAsync(
        string pattern,
        int bound,
        CancellationToken cancellationToken)
    {
        var rooms = await (
                from room in _dbContext.Rooms.AsNoTracking()
                join department in _dbContext.Departments.AsNoTracking()
                    on room.DepartmentId equals department.DepartmentId
                where room.RoomNumber.Contains(pattern)
                orderby room.RoomNumber, room.RoomCode
                select new
                {
                    room.RoomCode,
                    room.RoomNumber,
                    room.Description,
                    department.DepartmentCode
                })
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rooms.Count == 0)
        {
            return [];
        }

        HashSet<string> roomCodes = rooms.Select(room => room.RoomCode).ToHashSet(StringComparer.Ordinal);

        Dictionary<string, List<string>> keysByRoom = new(StringComparer.Ordinal);
        foreach (string roomCode in roomCodes)
        {
            IReadOnlyList<string> keyNumbers = await _accessResolution
                .ListKeyNumbersOpeningRoomAsync(roomCode, cancellationToken)
                .ConfigureAwait(false);
            keysByRoom[roomCode] = keyNumbers.ToList();
        }

        return rooms
            .Select(room => new GlobalRoomSearchHit(
                room.RoomCode,
                room.RoomNumber,
                room.Description,
                room.DepartmentCode,
                keysByRoom.TryGetValue(room.RoomCode, out List<string>? keys) ? keys : []))
            .ToArray();
    }

    private async Task<IReadOnlyList<GlobalKeyNumberSearchHit>> SearchKeyNumbersAsync(
        string pattern,
        int bound,
        CancellationToken cancellationToken)
    {
        List<string> keyNumbers = await _dbContext.KeyAccessPatterns.AsNoTracking()
            .Where(patternEntity => patternEntity.KeyNumber.Contains(pattern))
            .OrderBy(patternEntity => patternEntity.KeyNumber)
            .Select(patternEntity => patternEntity.KeyNumber)
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (keyNumbers.Count == 0)
        {
            return [];
        }

        HashSet<string> keySet = keyNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var patterns = await _dbContext.KeyAccessPatterns.AsNoTracking()
            .Where(patternEntity => keySet.Contains(patternEntity.KeyNumber))
            .Select(patternEntity => new
            {
                patternEntity.KeyNumber,
                patternEntity.Classification,
                patternEntity.RoomCode
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, KeyAccessClassification> classificationByKey = patterns.ToDictionary(
            item => item.KeyNumber,
            item => DomainCatalogMapper.ParseClassification(item.Classification),
            StringComparer.OrdinalIgnoreCase);

        var copies = await _dbContext.KeyAssets.AsNoTracking()
            .Where(key => keySet.Contains(key.KeyNumber))
            .OrderBy(key => key.KeyNumber)
            .ThenBy(key => key.MedecoKeyCode)
            .Select(key => new { key.KeyAssetId, key.KeyNumber, key.MedecoKeyCode, key.Condition })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, (string Availability, PartyHolderDisplay? Holder)> custody =
            await LoadOpenCustodyAsync(copies.Select(copy => copy.KeyAssetId), cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await _accessResolution.ResolveForPatternsAsync(
                    patterns.Select(item => new KeyAccessResolutionRequest(
                        item.KeyNumber,
                        DomainCatalogMapper.ParseClassification(item.Classification),
                        item.RoomCode)),
                    expandMaster: false,
                    cancellationToken)
                .ConfigureAwait(false);

        return keyNumbers
            .Select(keyNumber =>
            {
                IReadOnlyList<KeyOpenedRoomItem> rooms =
                    roomsByKey.TryGetValue(keyNumber, out IReadOnlyList<KeyOpenedRoomItem>? opened)
                        ? opened
                        : [];

                IReadOnlyList<GlobalPhysicalCopyState> copyStates = copies
                    .Where(copy => string.Equals(copy.KeyNumber, keyNumber, StringComparison.OrdinalIgnoreCase))
                    .Select(copy =>
                    {
                        KeyPhysicalCondition condition = DomainCatalogMapper.ParseCondition(copy.Condition);
                        bool isIssued = custody.ContainsKey(copy.KeyAssetId);
                        string availability = OperationalKeyAvailability.DeriveCustody(condition, isIssued);
                        PartyHolderDisplay? holder = isIssued ? custody[copy.KeyAssetId].Holder : null;
                        return new GlobalPhysicalCopyState(
                            copy.MedecoKeyCode,
                            condition,
                            availability,
                            holder);
                    })
                    .ToArray();

                KeyAccessClassification classification =
                    classificationByKey.TryGetValue(keyNumber, out KeyAccessClassification found)
                        ? found
                        : KeyAccessClassification.Regular;

                return new GlobalKeyNumberSearchHit(
                    keyNumber,
                    classification,
                    rooms,
                    copyStates);
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<GlobalMedecoSearchHit>> SearchMedecoCopiesAsync(
        string pattern,
        int bound,
        CancellationToken cancellationToken)
    {
        var copies = await (
                from key in _dbContext.KeyAssets.AsNoTracking()
                join access in _dbContext.KeyAccessPatterns.AsNoTracking()
                    on key.KeyNumber equals access.KeyNumber
                where key.MedecoKeyCode.Contains(pattern)
                orderby key.KeyNumber, key.MedecoKeyCode
                select new
                {
                    key.KeyAssetId,
                    key.KeyNumber,
                    key.MedecoKeyCode,
                    key.Condition,
                    access.Classification,
                    access.RoomCode
                })
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (copies.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, (string Availability, PartyHolderDisplay? Holder)> custody =
            await LoadOpenCustodyAsync(copies.Select(copy => copy.KeyAssetId), cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>> roomsByKey =
            await _accessResolution.ResolveForPatternsAsync(
                    copies
                        .GroupBy(copy => copy.KeyNumber, StringComparer.Ordinal)
                        .Select(group =>
                        {
                            var first = group.First();
                            return new KeyAccessResolutionRequest(
                                first.KeyNumber,
                                DomainCatalogMapper.ParseClassification(first.Classification),
                                first.RoomCode);
                        }),
                    expandMaster: false,
                    cancellationToken)
                .ConfigureAwait(false);

        return copies
            .Select(copy =>
            {
                IReadOnlyList<KeyOpenedRoomItem> rooms =
                    roomsByKey.TryGetValue(copy.KeyNumber, out IReadOnlyList<KeyOpenedRoomItem>? opened)
                        ? opened
                        : [];

                KeyAccessClassification classification =
                    DomainCatalogMapper.ParseClassification(copy.Classification);
                KeyPhysicalCondition condition = DomainCatalogMapper.ParseCondition(copy.Condition);
                bool isIssued = custody.ContainsKey(copy.KeyAssetId);
                string availability = OperationalKeyAvailability.DeriveCustody(condition, isIssued);
                PartyHolderDisplay? holder = isIssued ? custody[copy.KeyAssetId].Holder : null;

                return new GlobalMedecoSearchHit(
                    copy.KeyNumber,
                    copy.MedecoKeyCode,
                    classification,
                    condition,
                    availability,
                    holder,
                    rooms);
            })
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<KeyOpenedRoomItem>>> ResolveRoomsForKeyNumbersAsync(
        IEnumerable<string> keyNumbers,
        CancellationToken cancellationToken)
    {
        List<string> numbers = keyNumbers
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (numbers.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<KeyOpenedRoomItem>>(StringComparer.Ordinal);
        }

        var patterns = await _dbContext.KeyAccessPatterns.AsNoTracking()
            .Where(pattern => numbers.Contains(pattern.KeyNumber))
            .Select(pattern => new { pattern.KeyNumber, pattern.Classification, pattern.RoomCode })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _accessResolution.ResolveForPatternsAsync(
                patterns.Select(item => new KeyAccessResolutionRequest(
                    item.KeyNumber,
                    DomainCatalogMapper.ParseClassification(item.Classification),
                    item.RoomCode)),
                expandMaster: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, (string Availability, PartyHolderDisplay? Holder)>> LoadOpenCustodyAsync(
        IEnumerable<Guid> keyAssetIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = keyAssetIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var openLoans = await (
                from loan in _dbContext.Loans.AsNoTracking()
                join party in _dbContext.Parties.AsNoTracking()
                    on loan.BorrowerPartyReference equals party.PartyCode
                where loan.Status == nameof(LoanStatus.Open)
                    && ids.Contains(loan.KeyAssetId)
                select new
                {
                    loan.KeyAssetId,
                    party.FirstName,
                    party.LastName,
                    party.Uin
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return openLoans
            .GroupBy(item => item.KeyAssetId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var holder = group.First();
                    return (
                        OperationalKeyAvailability.Issued,
                        (PartyHolderDisplay?)new PartyHolderDisplay(
                            holder.FirstName,
                            holder.LastName,
                            holder.Uin));
                });
    }
}
