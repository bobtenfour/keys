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

    public Task<bool> DepartmentExistsAsync(string departmentCode, CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(item => item.DepartmentCode == department.DepartmentCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The department was not found in persistence.");
        }

        entity.IsActive = department.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Department?> FindDepartmentAsync(string departmentCode, CancellationToken cancellationToken)
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
            .Select(entity => new DepartmentListItem(entity.DepartmentCode, entity.IsActive))
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
        entity.IsActive = room.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
        return await _dbContext.Rooms.AsNoTracking()
            .OrderBy(entity => entity.RoomNumber)
            .ThenBy(entity => entity.RoomCode)
            .Select(entity => new RoomListItem(
                entity.RoomCode,
                entity.RoomNumber,
                entity.Description,
                entity.IsActive))
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
        entity.DepartmentCode = member.DepartmentCode;
        entity.Status = member.Status.ToString();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
                orderby party.LastName, party.FirstName, member.WorkforceMemberCode
                select new WorkforceMemberListItem(
                    member.WorkforceMemberCode,
                    member.PartyCode,
                    party.FirstName,
                    party.LastName,
                    party.Uin,
                    member.WorkforceType,
                    member.DepartmentCode,
                    member.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> WorkAssignmentExistsAsync(string workAssignmentCode, CancellationToken cancellationToken)
    {
        return _dbContext.WorkAssignments.AnyAsync(
            entity => entity.WorkAssignmentCode == workAssignmentCode,
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
            .FirstOrDefaultAsync(item => item.WorkAssignmentCode == assignment.WorkAssignmentCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The work assignment was not found in persistence.");
        }

        entity.IsPrimary = assignment.IsPrimary;
        entity.IsActive = assignment.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkAssignment?> FindWorkAssignmentAsync(
        string workAssignmentCode,
        CancellationToken cancellationToken)
    {
        WorkAssignmentEntity? entity = await _dbContext.WorkAssignments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.WorkAssignmentCode == workAssignmentCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task ClearPrimaryAssignmentsAsync(string workforceMemberCode, CancellationToken cancellationToken)
    {
        List<WorkAssignmentEntity> primaries = await _dbContext.WorkAssignments
            .Where(entity =>
                entity.WorkforceMemberCode == workforceMemberCode
                && entity.IsActive
                && entity.IsPrimary)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (WorkAssignmentEntity entity in primaries)
        {
            entity.IsPrimary = false;
        }

        if (primaries.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
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
            .ThenBy(entity => entity.WorkAssignmentCode)
            .Select(entity => new WorkAssignmentListItem(
                entity.WorkAssignmentCode,
                entity.WorkforceMemberCode,
                entity.RoomCode,
                entity.IsPrimary,
                entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
