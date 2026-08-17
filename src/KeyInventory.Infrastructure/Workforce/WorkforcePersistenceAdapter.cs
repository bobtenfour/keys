using KeyInventory.Application.Workforce;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.Workforce;

public sealed class WorkforcePersistenceAdapter : IWorkforcePersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;

    public WorkforcePersistenceAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<bool> PartyExistsAsync(string partyCode, CancellationToken cancellationToken)
    {
        return _dbContext.Parties.AnyAsync(entity => entity.PartyCode == partyCode, cancellationToken);
    }

    public Task<bool> PartyUinExistsAsync(string uin, CancellationToken cancellationToken)
    {
        return _dbContext.Parties.AnyAsync(entity => entity.Uin == uin, cancellationToken);
    }

    public async Task AddPartyAsync(Party party, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(party);
        _dbContext.Parties.Add(DomainWorkforceMapper.ToEntity(party));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdatePartyAsync(Party party, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(party);
        PartyEntity? entity = await _dbContext.Parties
            .FirstOrDefaultAsync(item => item.PartyCode == party.PartyCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The party was not found in persistence.");
        }

        entity.FirstName = party.FirstName;
        entity.LastName = party.LastName;
        entity.Uin = party.Uin;
        entity.IsActive = party.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddPartyAndWorkforceMemberAsync(
        Party party,
        WorkforceMember member,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(member);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _dbContext.Parties.Add(DomainWorkforceMapper.ToEntity(party));
            _dbContext.WorkforceMembers.Add(DomainWorkforceMapper.ToEntity(member));
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<PartyListItem>> ListPartiesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Parties.AsNoTracking()
            .OrderBy(entity => entity.LastName)
            .ThenBy(entity => entity.FirstName)
            .ThenBy(entity => entity.Uin)
            .Select(entity => new PartyListItem(
                entity.PartyCode,
                entity.FirstName,
                entity.LastName,
                entity.Uin))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Party?> FindPartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        PartyEntity? entity = await _dbContext.Parties.AsNoTracking()
            .FirstOrDefaultAsync(item => item.PartyCode == partyCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<Party?> FindPartyByUinAsync(string uin, CancellationToken cancellationToken)
    {
        PartyEntity? entity = await _dbContext.Parties.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Uin == uin, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public Task<bool> DepartmentExistsByCodeAsync(string departmentCode, CancellationToken cancellationToken)
    {
        return _dbContext.Departments.AnyAsync(entity => entity.DepartmentCode == departmentCode, cancellationToken);
    }

    public async Task AddDepartmentAsync(Department department, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);
        _dbContext.Departments.Add(DomainWorkforceMapper.ToEntity(department));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateDepartmentAsync(Department department, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);
        DepartmentEntity? entity = await _dbContext.Departments
            .FirstOrDefaultAsync(item => item.DepartmentId == department.DepartmentId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The department was not found in persistence.");
        }

        entity.DepartmentCode = department.DepartmentCode;
        entity.IsActive = department.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        DepartmentEntity? entity = await _dbContext.Departments
            .FirstOrDefaultAsync(item => item.DepartmentId == departmentId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The department was not found in persistence.");
        }

        _dbContext.Departments.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountWorkforceMembersForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkforceMembers.CountAsync(
            entity => entity.DepartmentId == departmentId,
            cancellationToken);
    }

    public Task<int> CountRoomsForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Rooms.CountAsync(
            entity => entity.DepartmentId == departmentId,
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

    public async Task<Department?> FindDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        DepartmentEntity? entity = await _dbContext.Departments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.DepartmentId == departmentId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<Department?> FindDepartmentByCodeAsync(
        string departmentCode,
        CancellationToken cancellationToken)
    {
        DepartmentEntity? entity = await _dbContext.Departments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.DepartmentCode == departmentCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<DepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Departments.AsNoTracking()
            .OrderBy(entity => entity.DepartmentCode)
            .Select(entity => new DepartmentListItem(
                entity.DepartmentId,
                entity.DepartmentCode,
                entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> RoomExistsAsync(string roomCode, CancellationToken cancellationToken)
    {
        return _dbContext.Rooms.AnyAsync(entity => entity.RoomCode == roomCode, cancellationToken);
    }

    public Task<bool> RoomNumberExistsAsync(string roomNumber, CancellationToken cancellationToken)
    {
        return _dbContext.Rooms.AnyAsync(entity => entity.RoomNumber == roomNumber, cancellationToken);
    }

    public async Task AddRoomAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);
        _dbContext.Rooms.Add(DomainWorkforceMapper.ToEntity(room));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateRoomAsync(Room room, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(room);
        RoomEntity? entity = await _dbContext.Rooms
            .FirstOrDefaultAsync(item => item.RoomCode == room.RoomCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The room was not found in persistence.");
        }

        entity.RoomNumber = room.RoomNumber;
        entity.Description = room.Description;
        entity.DepartmentId = room.DepartmentId;
        entity.IsActive = room.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        RoomEntity? entity = await _dbContext.Rooms
            .FirstOrDefaultAsync(item => item.RoomCode == roomCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The room was not found in persistence.");
        }

        _dbContext.Rooms.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountWorkAssignmentsForRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        return _dbContext.WorkAssignments.CountAsync(
            entity => entity.RoomCode == roomCode,
            cancellationToken);
    }

    public async Task<Room?> FindRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        RoomEntity? entity = await _dbContext.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoomCode == roomCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<RoomListItem>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        return await (
                from room in _dbContext.Rooms.AsNoTracking()
                join department in _dbContext.Departments.AsNoTracking()
                    on room.DepartmentId equals department.DepartmentId
                orderby room.RoomNumber, room.RoomCode
                select new RoomListItem(
                    room.RoomCode,
                    room.RoomNumber,
                    room.Description,
                    room.DepartmentId,
                    department.DepartmentCode,
                    room.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountWorkforceMembersAsync(CancellationToken cancellationToken)
    {
        return _dbContext.WorkforceMembers.CountAsync(cancellationToken);
    }

    public Task<bool> WorkforceMemberExistsAsync(string workforceMemberCode, CancellationToken cancellationToken)
    {
        return _dbContext.WorkforceMembers.AnyAsync(
            entity => entity.WorkforceMemberCode == workforceMemberCode,
            cancellationToken);
    }

    public Task<bool> ActiveWorkforceMemberExistsForPartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        return _dbContext.WorkforceMembers.AnyAsync(
            entity => entity.PartyCode == partyCode && entity.Status == nameof(WorkforceMemberStatus.Active),
            cancellationToken);
    }

    public async Task AddWorkforceMemberAsync(WorkforceMember member, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(member);
        _dbContext.WorkforceMembers.Add(DomainWorkforceMapper.ToEntity(member));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateWorkforceMemberAsync(WorkforceMember member, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(member);
        WorkforceMemberEntity? entity = await _dbContext.WorkforceMembers
            .FirstOrDefaultAsync(item => item.WorkforceMemberCode == member.WorkforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The workforce member was not found in persistence.");
        }

        entity.WorkforceType = member.WorkforceType.ToString();
        entity.DepartmentId = member.DepartmentId;
        entity.Status = member.Status.ToString();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        WorkforceMemberEntity? entity = await _dbContext.WorkforceMembers
            .FirstOrDefaultAsync(item => item.WorkforceMemberCode == workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The workforce member was not found in persistence.");
        }

        _dbContext.WorkforceMembers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        PartyEntity? entity = await _dbContext.Parties
            .FirstOrDefaultAsync(item => item.PartyCode == partyCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The party was not found in persistence.");
        }

        _dbContext.Parties.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountWorkforceMembersForPartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        return _dbContext.WorkforceMembers.CountAsync(
            entity => entity.PartyCode == partyCode,
            cancellationToken);
    }

    public Task<int> CountWorkAssignmentsForMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkAssignments.CountAsync(
            entity => entity.WorkforceMemberCode == workforceMemberCode,
            cancellationToken);
    }

    public async Task<WorkforceMember?> FindWorkforceMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        WorkforceMemberEntity? entity = await _dbContext.WorkforceMembers.AsNoTracking()
            .FirstOrDefaultAsync(item => item.WorkforceMemberCode == workforceMemberCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<WorkforceMemberListItem>> ListWorkforceMembersAsync(
        CancellationToken cancellationToken)
    {
        return await (
                from member in _dbContext.WorkforceMembers.AsNoTracking()
                join party in _dbContext.Parties.AsNoTracking() on member.PartyCode equals party.PartyCode
                join department in _dbContext.Departments.AsNoTracking()
                    on member.DepartmentId equals department.DepartmentId
                orderby party.LastName, party.FirstName, member.WorkforceMemberCode
                select new WorkforceMemberListItem(
                    member.WorkforceMemberCode,
                    member.PartyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    member.WorkforceType,
                    department.DepartmentCode,
                    member.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EligibleKeyHolderCandidate>> SearchEligibleKeyHoldersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (maxResults < 1)
        {
            return [];
        }

        string term = (searchText ?? string.Empty).Trim();
        int bound = Math.Min(maxResults, ISearchEligibleKeyHoldersUseCase.DefaultMaxResults);

        var query =
            from member in _dbContext.WorkforceMembers.AsNoTracking()
            join party in _dbContext.Parties.AsNoTracking() on member.PartyCode equals party.PartyCode
            join department in _dbContext.Departments.AsNoTracking()
                on member.DepartmentId equals department.DepartmentId
            where member.Status == nameof(WorkforceMemberStatus.Active)
                && party.IsActive
                && department.IsActive
                && _dbContext.WorkAssignments.Any(assignment =>
                    assignment.WorkforceMemberCode == member.WorkforceMemberCode
                    && assignment.IsActive)
            select new { member, party, department };

        if (term.Length > 0)
        {
            query = query.Where(item =>
                item.party.FirstName.Contains(term)
                || item.party.LastName.Contains(term)
                || (item.party.FirstName + " " + item.party.LastName).Contains(term)
                || item.party.Uin.Contains(term));
        }

        return await query
            .OrderBy(item => item.party.LastName)
            .ThenBy(item => item.party.FirstName)
            .ThenBy(item => item.member.WorkforceMemberCode)
            .Select(item => new EligibleKeyHolderCandidate(
                item.member.WorkforceMemberCode,
                item.party.FirstName,
                item.party.LastName,
                item.party.Uin,
                item.department.DepartmentCode))
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EligibleKeyHolderCandidate>> SearchActiveWorkforceMembersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (maxResults < 1)
        {
            return [];
        }

        string term = (searchText ?? string.Empty).Trim();
        int bound = Math.Min(maxResults, ISearchActiveWorkforceMembersUseCase.DefaultMaxResults);

        var query =
            from member in _dbContext.WorkforceMembers.AsNoTracking()
            join party in _dbContext.Parties.AsNoTracking() on member.PartyCode equals party.PartyCode
            join department in _dbContext.Departments.AsNoTracking()
                on member.DepartmentId equals department.DepartmentId
            where member.Status == nameof(WorkforceMemberStatus.Active)
            select new { member, party, department };

        if (term.Length > 0)
        {
            query = query.Where(item =>
                item.party.FirstName.Contains(term)
                || item.party.LastName.Contains(term)
                || (item.party.FirstName + " " + item.party.LastName).Contains(term)
                || item.party.Uin.Contains(term));
        }

        return await query
            .OrderBy(item => item.party.LastName)
            .ThenBy(item => item.party.FirstName)
            .ThenBy(item => item.member.WorkforceMemberCode)
            .Select(item => new EligibleKeyHolderCandidate(
                item.member.WorkforceMemberCode,
                item.party.FirstName,
                item.party.LastName,
                item.party.Uin,
                item.department.DepartmentCode))
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoomListItem>> SearchActiveRoomsAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (maxResults < 1)
        {
            return [];
        }

        string term = (searchText ?? string.Empty).Trim();
        int bound = Math.Min(maxResults, ISearchActiveRoomsUseCase.DefaultMaxResults);

        var query =
            from room in _dbContext.Rooms.AsNoTracking()
            join department in _dbContext.Departments.AsNoTracking()
                on room.DepartmentId equals department.DepartmentId
            where room.IsActive
            select new { room, department };

        if (term.Length > 0)
        {
            query = query.Where(item =>
                item.room.RoomNumber.Contains(term) || item.room.Description.Contains(term));
        }

        return await query
            .OrderBy(item => item.room.RoomNumber)
            .ThenBy(item => item.room.RoomCode)
            .Select(item => new RoomListItem(
                item.room.RoomCode,
                item.room.RoomNumber,
                item.room.Description,
                item.room.DepartmentId,
                item.department.DepartmentCode,
                item.room.IsActive))
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoomListItem>> SearchActiveRoomsInDepartmentAsync(
        Guid departmentId,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty || maxResults < 1)
        {
            return [];
        }

        string term = (searchText ?? string.Empty).Trim();
        int bound = Math.Min(maxResults, ISearchActiveRoomsUseCase.DefaultMaxResults);

        var query =
            from room in _dbContext.Rooms.AsNoTracking()
            join department in _dbContext.Departments.AsNoTracking()
                on room.DepartmentId equals department.DepartmentId
            where room.IsActive && room.DepartmentId == departmentId
            select new { room, department };

        if (term.Length > 0)
        {
            query = query.Where(item =>
                item.room.RoomNumber.Contains(term) || item.room.Description.Contains(term));
        }

        return await query
            .OrderBy(item => item.room.RoomNumber)
            .ThenBy(item => item.room.RoomCode)
            .Select(item => new RoomListItem(
                item.room.RoomCode,
                item.room.RoomNumber,
                item.room.Description,
                item.room.DepartmentId,
                item.department.DepartmentCode,
                item.room.IsActive))
            .Take(bound)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ActiveWorkAssignmentWithRoomDepartment>>
        ListActiveWorkAssignmentsWithRoomDepartmentAsync(
            string workforceMemberCode,
            CancellationToken cancellationToken)
    {
        return await (
                from assignment in _dbContext.WorkAssignments.AsNoTracking()
                join room in _dbContext.Rooms.AsNoTracking() on assignment.RoomCode equals room.RoomCode
                join department in _dbContext.Departments.AsNoTracking()
                    on room.DepartmentId equals department.DepartmentId
                where assignment.WorkforceMemberCode == workforceMemberCode
                    && assignment.IsActive
                select new ActiveWorkAssignmentWithRoomDepartment(
                    assignment.WorkAssignmentId,
                    assignment.RoomCode,
                    room.DepartmentId,
                    department.DepartmentCode))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ActiveWorkAssignmentExistsAsync(
        string workforceMemberCode,
        string roomCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.WorkAssignments.AnyAsync(
            entity => entity.WorkforceMemberCode == workforceMemberCode
                && entity.RoomCode == roomCode
                && entity.IsActive,
            cancellationToken);
    }

    public async Task AddWorkAssignmentAsync(WorkAssignment assignment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        _dbContext.WorkAssignments.Add(DomainWorkforceMapper.ToEntity(assignment));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateWorkAssignmentAsync(WorkAssignment assignment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        WorkAssignmentEntity? entity = await _dbContext.WorkAssignments
            .FirstOrDefaultAsync(item => item.WorkAssignmentId == assignment.WorkAssignmentId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The work assignment was not found in persistence.");
        }

        entity.IsActive = assignment.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteWorkAssignmentAsync(Guid workAssignmentId, CancellationToken cancellationToken)
    {
        WorkAssignmentEntity? entity = await _dbContext.WorkAssignments
            .FirstOrDefaultAsync(item => item.WorkAssignmentId == workAssignmentId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The work assignment was not found in persistence.");
        }

        _dbContext.WorkAssignments.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkAssignment?> FindWorkAssignmentAsync(
        Guid workAssignmentId,
        CancellationToken cancellationToken)
    {
        WorkAssignmentEntity? entity = await _dbContext.WorkAssignments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.WorkAssignmentId == workAssignmentId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<WorkAssignment>> ListActiveWorkAssignmentsAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken)
    {
        List<WorkAssignmentEntity> entities = await _dbContext.WorkAssignments.AsNoTracking()
            .Where(entity => entity.WorkforceMemberCode == workforceMemberCode && entity.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(DomainWorkforceMapper.ToDomain).ToArray();
    }

    public async Task<IReadOnlyList<WorkAssignmentListItem>> ListWorkAssignmentsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.WorkAssignments.AsNoTracking()
            .OrderBy(entity => entity.WorkforceMemberCode)
            .ThenBy(entity => entity.RoomCode)
            .Select(entity => new WorkAssignmentListItem(
                entity.WorkAssignmentId,
                entity.WorkforceMemberCode,
                entity.RoomCode,
                entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
