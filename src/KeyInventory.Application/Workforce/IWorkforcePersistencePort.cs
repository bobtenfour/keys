using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;

namespace KeyInventory.Application.Workforce;

public interface IWorkforcePersistencePort
{
    Task<bool> PartyExistsAsync(string partyCode, CancellationToken cancellationToken);

    Task<bool> PartyUinExistsAsync(string uin, CancellationToken cancellationToken);

    Task AddPartyAsync(Party party, CancellationToken cancellationToken);

    Task UpdatePartyAsync(Party party, CancellationToken cancellationToken);

    /// <summary>
    /// Persists Party and WorkforceMember in one SQL Server transaction (all-or-nothing).
    /// </summary>
    Task AddPartyAndWorkforceMemberAsync(Party party, WorkforceMember member, CancellationToken cancellationToken);

    Task<Party?> FindPartyAsync(string partyCode, CancellationToken cancellationToken);

    Task<Party?> FindPartyByUinAsync(string uin, CancellationToken cancellationToken);

    Task<IReadOnlyList<PartyListItem>> ListPartiesAsync(CancellationToken cancellationToken);

    Task<bool> DepartmentExistsByCodeAsync(string departmentCode, CancellationToken cancellationToken);

    Task AddDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task UpdateDepartmentAsync(Department department, CancellationToken cancellationToken);

    Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<int> CountWorkforceMembersForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<int> CountRoomsForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<int> CountLoansJustifiedByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<Department?> FindDepartmentAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<Department?> FindDepartmentByCodeAsync(string departmentCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentListItem>> ListDepartmentsAsync(CancellationToken cancellationToken);

    Task<bool> RoomExistsAsync(string roomCode, CancellationToken cancellationToken);

    Task<bool> RoomNumberExistsAsync(string roomNumber, CancellationToken cancellationToken);

    Task AddRoomAsync(Room room, CancellationToken cancellationToken);

    Task UpdateRoomAsync(Room room, CancellationToken cancellationToken);

    Task DeleteRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task<int> CountWorkAssignmentsForRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task<Room?> FindRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoomListItem>> ListRoomsAsync(CancellationToken cancellationToken);

    Task<int> CountWorkforceMembersAsync(CancellationToken cancellationToken);

    Task<bool> WorkforceMemberExistsAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task<bool> ActiveWorkforceMemberExistsForPartyAsync(string partyCode, CancellationToken cancellationToken);

    Task AddWorkforceMemberAsync(WorkforceMember member, CancellationToken cancellationToken);

    Task UpdateWorkforceMemberAsync(WorkforceMember member, CancellationToken cancellationToken);

    Task DeleteWorkforceMemberAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task DeletePartyAsync(string partyCode, CancellationToken cancellationToken);

    Task<int> CountWorkforceMembersForPartyAsync(string partyCode, CancellationToken cancellationToken);

    Task<int> CountWorkAssignmentsForMemberAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<WorkforceMember?> FindWorkforceMemberAsync(string workforceMemberCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceMemberListItem>> ListWorkforceMembersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Bounded name/UIN search of Active workforce members that may be issue candidates.
    /// Application applies Domain eligibility; this port must not return unbounded workforce sets.
    /// </summary>
    Task<IReadOnlyList<EligibleKeyHolderCandidate>> SearchEligibleKeyHoldersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bounded name/UIN search of Active workforce members (no work-assignment eligibility filter).
    /// Empty search text returns the first <paramref name="maxResults"/> active members
    /// ordered by name.
    /// </summary>
    Task<IReadOnlyList<EligibleKeyHolderCandidate>> SearchActiveWorkforceMembersAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bounded RoomNumber/Description search of active rooms.
    /// Empty search text returns the first <paramref name="maxResults"/> active rooms
    /// ordered by RoomNumber. Results always include DepartmentId + DepartmentCode.
    /// </summary>
    Task<IReadOnlyList<RoomListItem>> SearchActiveRoomsAsync(
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    /// <summary>
    /// Bounded RoomNumber/Description search of active rooms restricted to a single Department.
    /// Empty search text returns the first <paramref name="maxResults"/> active rooms in that
    /// Department ordered by RoomNumber.
    /// </summary>
    Task<IReadOnlyList<RoomListItem>> SearchActiveRoomsInDepartmentAsync(
        Guid departmentId,
        string searchText,
        int maxResults,
        CancellationToken cancellationToken);

    /// <summary>
    /// Active work assignments for a workforce member, with each assignment's Room DepartmentId.
    /// Used by department reassignment guards to enforce Room.DepartmentId == Member.DepartmentId.
    /// </summary>
    Task<IReadOnlyList<ActiveWorkAssignmentWithRoomDepartment>> ListActiveWorkAssignmentsWithRoomDepartmentAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<bool> ActiveWorkAssignmentExistsAsync(
        string workforceMemberCode,
        string roomCode,
        CancellationToken cancellationToken);

    Task AddWorkAssignmentAsync(WorkAssignment assignment, CancellationToken cancellationToken);

    Task UpdateWorkAssignmentAsync(WorkAssignment assignment, CancellationToken cancellationToken);

    Task DeleteWorkAssignmentAsync(Guid workAssignmentId, CancellationToken cancellationToken);

    Task<WorkAssignment?> FindWorkAssignmentAsync(Guid workAssignmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkAssignment>> ListActiveWorkAssignmentsAsync(
        string workforceMemberCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkAssignmentListItem>> ListWorkAssignmentsAsync(CancellationToken cancellationToken);
}
