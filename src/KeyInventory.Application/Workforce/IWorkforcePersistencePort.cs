using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface IWorkforcePersistencePort
{
    Task<bool> PartyExistsAsync(string partyCode, CancellationToken cancellationToken);

    Task<bool> PartyUinExistsAsync(string uin, CancellationToken cancellationToken);

    Task AddPartyAsync(Party party, CancellationToken cancellationToken);

    /// <summary>
    /// Persists Party and WorkforceMember in one SQL Server transaction (all-or-nothing).
    /// </summary>
    Task AddPartyAndWorkforceMemberAsync(Party party, WorkforceMember member, CancellationToken cancellationToken);

    /// <summary>
    /// Persists two Parties and two WorkforceMembers in one SQL Server transaction (all-or-nothing).
    /// </summary>
    Task AddBootstrapPartiesAndWorkforceMembersAsync(
        Party firstParty,
        Party secondParty,
        WorkforceMember firstMember,
        WorkforceMember secondMember,
        CancellationToken cancellationToken);

    Task<Party?> FindPartyAsync(string partyCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<PartyListItem>> ListPartiesAsync(CancellationToken cancellationToken);

    Task<bool> OrganizationExistsAsync(string organizationCode, CancellationToken cancellationToken);

    Task AddOrganizationAsync(Organization organization, CancellationToken cancellationToken);

    Task UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken);

    Task<Organization?> FindOrganizationAsync(string organizationCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizationListItem>> ListOrganizationsAsync(CancellationToken cancellationToken);

    Task<bool> DepartmentExistsAsync(string organizationCode, string departmentCode, CancellationToken cancellationToken);

    Task AddDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task UpdateDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task<Department?> FindDepartmentAsync(string organizationCode, string departmentCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken);

    Task<bool> BuildingExistsAsync(string buildingCode, CancellationToken cancellationToken);

    Task AddBuildingAsync(Building building, CancellationToken cancellationToken);

    Task UpdateBuildingAsync(Building building, CancellationToken cancellationToken);

    Task<Building?> FindBuildingAsync(string buildingCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<BuildingListItem>> ListBuildingsAsync(CancellationToken cancellationToken);

    Task<bool> RoomExistsAsync(string roomCode, CancellationToken cancellationToken);

    Task<bool> RoomNumberExistsInBuildingAsync(string buildingCode, string roomNumber, CancellationToken cancellationToken);

    Task AddRoomAsync(Room room, CancellationToken cancellationToken);

    Task UpdateRoomAsync(Room room, CancellationToken cancellationToken);

    Task<Room?> FindRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomListItem>> ListRoomsAsync(CancellationToken cancellationToken);

    Task<int> CountWorkforceMembersAsync(CancellationToken cancellationToken);

    Task<bool> WorkforceMemberExistsAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task<bool> ActiveWorkforceMemberExistsForPartyAsync(string partyCode, CancellationToken cancellationToken);

    Task AddWorkforceMemberAsync(WorkforceMember member, CancellationToken cancellationToken);

    Task UpdateWorkforceMemberAsync(WorkforceMember member, CancellationToken cancellationToken);

    Task AddWorkforceMembersAsync(IReadOnlyList<WorkforceMember> members, CancellationToken cancellationToken);

    Task<WorkforceMember?> FindWorkforceMemberAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberListItem>> ListWorkforceMembersAsync(CancellationToken cancellationToken);

    Task<bool> WorkAssignmentExistsAsync(string workAssignmentCode, CancellationToken cancellationToken);

    Task AddWorkAssignmentAsync(WorkAssignment assignment, CancellationToken cancellationToken);

    Task UpdateWorkAssignmentAsync(WorkAssignment assignment, CancellationToken cancellationToken);

    Task<WorkAssignment?> FindWorkAssignmentAsync(string workAssignmentCode, CancellationToken cancellationToken);

    Task ClearPrimaryAssignmentsAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkAssignment>> ListActiveWorkAssignmentsAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkAssignmentListItem>> ListWorkAssignmentsAsync(CancellationToken cancellationToken);
}
