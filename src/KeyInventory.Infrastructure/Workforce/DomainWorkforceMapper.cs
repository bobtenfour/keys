using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure.Data;

namespace KeyInventory.Infrastructure.Workforce;

internal static class DomainWorkforceMapper
{
    internal static PartyEntity ToEntity(Party party)
    {
        return new PartyEntity
        {
            PartyCode = party.PartyCode,
            FirstName = party.FirstName,
            LastName = party.LastName,
            Uin = party.Uin,
            IsActive = party.IsActive
        };
    }

    internal static Party ToDomain(PartyEntity entity)
    {
        Party party = new(entity.PartyCode, entity.FirstName, entity.LastName, entity.Uin);
        if (!entity.IsActive)
        {
            party.Retire();
        }

        return party;
    }

    internal static OrganizationEntity ToEntity(Organization organization)
    {
        return new OrganizationEntity
        {
            OrganizationCode = organization.OrganizationCode,
            IsActive = organization.IsActive
        };
    }

    internal static Organization ToDomain(OrganizationEntity entity)
    {
        Organization organization = new(entity.OrganizationCode);
        if (!entity.IsActive)
        {
            organization.Retire();
        }

        return organization;
    }

    internal static DepartmentEntity ToEntity(Department department)
    {
        return new DepartmentEntity
        {
            OrganizationCode = department.OrganizationCode,
            DepartmentCode = department.DepartmentCode,
            IsActive = department.IsActive
        };
    }

    internal static Department ToDomain(DepartmentEntity entity, Organization organization)
    {
        Department department = new(entity.DepartmentCode, organization);
        if (!entity.IsActive)
        {
            department.Retire();
        }

        return department;
    }

    internal static BuildingEntity ToEntity(Building building)
    {
        return new BuildingEntity
        {
            BuildingCode = building.BuildingCode,
            IsActive = building.IsActive
        };
    }

    internal static Building ToDomain(BuildingEntity entity)
    {
        Building building = new(entity.BuildingCode);
        if (!entity.IsActive)
        {
            building.Retire();
        }

        return building;
    }

    internal static RoomEntity ToEntity(Room room)
    {
        return new RoomEntity
        {
            RoomCode = room.RoomCode,
            BuildingCode = room.BuildingCode,
            RoomNumber = room.RoomNumber,
            Description = room.Description,
            IsActive = room.IsActive
        };
    }

    internal static Room ToDomain(RoomEntity entity, Building building)
    {
        Room room = new(entity.RoomCode, building, entity.RoomNumber, entity.Description);
        if (!entity.IsActive)
        {
            room.Retire();
        }

        return room;
    }

    internal static WorkforceMemberEntity ToEntity(WorkforceMember member)
    {
        return new WorkforceMemberEntity
        {
            WorkforceMemberCode = member.WorkforceMemberCode,
            PartyCode = member.PartyCode,
            WorkforceType = member.WorkforceType.ToString(),
            OrganizationCode = member.OrganizationCode,
            DepartmentCode = member.DepartmentCode,
            ResponsibleManagerWorkforceMemberCode = member.ResponsibleManagerWorkforceMemberCode,
            Status = member.Status.ToString()
        };
    }

    internal static WorkforceMember ToDomain(WorkforceMemberEntity entity)
    {
        WorkforceType workforceType = Enum.Parse<WorkforceType>(entity.WorkforceType);
        WorkforceMember member = new(
            entity.WorkforceMemberCode,
            entity.PartyCode,
            workforceType,
            entity.OrganizationCode,
            entity.DepartmentCode,
            entity.ResponsibleManagerWorkforceMemberCode);

        if (string.Equals(entity.Status, nameof(WorkforceMemberStatus.Terminated), StringComparison.Ordinal))
        {
            member.Terminate();
        }

        return member;
    }

    internal static WorkAssignmentEntity ToEntity(WorkAssignment assignment)
    {
        return new WorkAssignmentEntity
        {
            WorkAssignmentCode = assignment.WorkAssignmentCode,
            WorkforceMemberCode = assignment.WorkforceMemberCode,
            RoomCode = assignment.RoomCode,
            IsPrimary = assignment.IsPrimary,
            IsActive = assignment.IsActive
        };
    }

    internal static WorkAssignment ToDomain(WorkAssignmentEntity entity)
    {
        WorkAssignment assignment = new(
            entity.WorkAssignmentCode,
            entity.WorkforceMemberCode,
            entity.RoomCode,
            entity.IsPrimary);

        if (!entity.IsActive)
        {
            assignment.End();
        }
        else if (!entity.IsPrimary)
        {
            assignment.ClearPrimary();
        }

        return assignment;
    }
}
