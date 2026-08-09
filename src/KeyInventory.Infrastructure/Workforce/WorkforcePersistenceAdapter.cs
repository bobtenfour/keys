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

    public async Task<Party?> FindPartyAsync(string partyCode, CancellationToken cancellationToken)
    {
        PartyEntity? entity = await _dbContext.Parties.AsNoTracking()
            .FirstOrDefaultAsync(item => item.PartyCode == partyCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public Task<bool> OrganizationExistsAsync(string organizationCode, CancellationToken cancellationToken)
    {
        return _dbContext.Organizations.AnyAsync(entity => entity.OrganizationCode == organizationCode, cancellationToken);
    }

    public async Task AddOrganizationAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        _dbContext.Organizations.Add(DomainWorkforceMapper.ToEntity(organization));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        OrganizationEntity? entity = await _dbContext.Organizations
            .FirstOrDefaultAsync(item => item.OrganizationCode == organization.OrganizationCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The organization was not found in persistence.");
        }

        entity.IsActive = organization.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Organization?> FindOrganizationAsync(string organizationCode, CancellationToken cancellationToken)
    {
        OrganizationEntity? entity = await _dbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(item => item.OrganizationCode == organizationCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<OrganizationListItem>> ListOrganizationsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Organizations.AsNoTracking()
            .OrderBy(entity => entity.OrganizationCode)
            .Select(entity => new OrganizationListItem(entity.OrganizationCode, entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> DepartmentExistsAsync(
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.Departments.AnyAsync(
            entity => entity.OrganizationCode == organizationCode && entity.DepartmentCode == departmentCode,
            cancellationToken);
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
            .FirstOrDefaultAsync(
                item => item.OrganizationCode == department.OrganizationCode
                    && item.DepartmentCode == department.DepartmentCode,
                cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The department was not found in persistence.");
        }

        entity.IsActive = department.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Department?> FindDepartmentAsync(
        string organizationCode,
        string departmentCode,
        CancellationToken cancellationToken)
    {
        DepartmentEntity? entity = await _dbContext.Departments.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.OrganizationCode == organizationCode && item.DepartmentCode == departmentCode,
                cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return null;
        }

        Organization? organization = await FindOrganizationAsync(entity.OrganizationCode, cancellationToken)
            .ConfigureAwait(false);
        return organization is null ? null : DomainWorkforceMapper.ToDomain(entity, organization);
    }

    public async Task<IReadOnlyList<DepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Departments.AsNoTracking()
            .OrderBy(entity => entity.OrganizationCode)
            .ThenBy(entity => entity.DepartmentCode)
            .Select(entity => new DepartmentListItem(entity.OrganizationCode, entity.DepartmentCode, entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> BuildingExistsAsync(string buildingCode, CancellationToken cancellationToken)
    {
        return _dbContext.Buildings.AnyAsync(entity => entity.BuildingCode == buildingCode, cancellationToken);
    }

    public async Task AddBuildingAsync(Building building, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(building);
        _dbContext.Buildings.Add(DomainWorkforceMapper.ToEntity(building));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateBuildingAsync(Building building, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(building);
        BuildingEntity? entity = await _dbContext.Buildings
            .FirstOrDefaultAsync(item => item.BuildingCode == building.BuildingCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException("The building was not found in persistence.");
        }

        entity.IsActive = building.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Building?> FindBuildingAsync(string buildingCode, CancellationToken cancellationToken)
    {
        BuildingEntity? entity = await _dbContext.Buildings.AsNoTracking()
            .FirstOrDefaultAsync(item => item.BuildingCode == buildingCode, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : DomainWorkforceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<BuildingListItem>> ListBuildingsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Buildings.AsNoTracking()
            .OrderBy(entity => entity.BuildingCode)
            .Select(entity => new BuildingListItem(entity.BuildingCode, entity.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> RoomExistsAsync(string roomCode, CancellationToken cancellationToken)
    {
        return _dbContext.Rooms.AnyAsync(entity => entity.RoomCode == roomCode, cancellationToken);
    }

    public Task<bool> RoomNumberExistsInBuildingAsync(
        string buildingCode,
        string roomNumber,
        CancellationToken cancellationToken)
    {
        return _dbContext.Rooms.AnyAsync(
            entity => entity.BuildingCode == buildingCode && entity.RoomNumber == roomNumber,
            cancellationToken);
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

        entity.IsActive = room.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Room?> FindRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        RoomEntity? entity = await _dbContext.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoomCode == roomCode, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return null;
        }

        Building? building = await FindBuildingAsync(entity.BuildingCode, cancellationToken).ConfigureAwait(false);
        return building is null ? null : DomainWorkforceMapper.ToDomain(entity, building);
    }

    public async Task<IReadOnlyList<RoomListItem>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Rooms.AsNoTracking()
            .OrderBy(entity => entity.BuildingCode)
            .ThenBy(entity => entity.RoomNumber)
            .Select(entity => new RoomListItem(
                entity.RoomCode,
                entity.BuildingCode,
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
        entity.OrganizationCode = member.OrganizationCode;
        entity.DepartmentCode = member.DepartmentCode;
        entity.ResponsibleManagerWorkforceMemberCode = member.ResponsibleManagerWorkforceMemberCode;
        entity.Status = member.Status.ToString();
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddWorkforceMembersAsync(IReadOnlyList<WorkforceMember> members, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(members);
        _dbContext.WorkforceMembers.AddRange(members.Select(DomainWorkforceMapper.ToEntity));
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
        return await _dbContext.WorkforceMembers.AsNoTracking()
            .OrderBy(entity => entity.WorkforceMemberCode)
            .Select(entity => new WorkforceMemberListItem(
                entity.WorkforceMemberCode,
                entity.PartyCode,
                entity.WorkforceType,
                entity.OrganizationCode,
                entity.DepartmentCode,
                entity.ResponsibleManagerWorkforceMemberCode,
                entity.Status))
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
