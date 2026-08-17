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

    internal static DepartmentEntity ToEntity(Department department)
    {
        return new DepartmentEntity
        {
            DepartmentId = department.DepartmentId,
            DepartmentCode = department.DepartmentCode,
            IsActive = department.IsActive
        };
    }

    internal static Department ToDomain(DepartmentEntity entity)
    {
        Department department = new(entity.DepartmentId, entity.DepartmentCode);
        if (!entity.IsActive)
        {
            department.Retire();
        }

        return department;
    }

    internal static RoomEntity ToEntity(Room room)
    {
        return new RoomEntity
        {
            RoomCode = room.RoomCode,
            RoomNumber = room.RoomNumber,
            Description = room.Description,
            DepartmentId = room.DepartmentId,
            IsActive = room.IsActive
        };
    }

    internal static Room ToDomain(RoomEntity entity)
    {
        Room room = new(entity.RoomCode, entity.RoomNumber, entity.DepartmentId, entity.Description);
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
            DepartmentId = member.DepartmentId,
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
            entity.DepartmentId);

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
            WorkAssignmentId = assignment.WorkAssignmentId,
            WorkforceMemberCode = assignment.WorkforceMemberCode,
            RoomCode = assignment.RoomCode,
            IsActive = assignment.IsActive
        };
    }

    internal static WorkAssignment ToDomain(WorkAssignmentEntity entity)
    {
        WorkAssignment assignment = new(
            entity.WorkAssignmentId,
            entity.WorkforceMemberCode,
            entity.RoomCode);

        if (!entity.IsActive)
        {
            assignment.End();
        }

        return assignment;
    }
}
