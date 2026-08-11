using KeyInventory.Application.Workforce;
using Microsoft.Extensions.DependencyInjection;

namespace KeyInventory.ArchitectureTests;

internal static class WorkforceEligibilityTestFixture
{
    internal static async Task<(string MemberCode, string PartyCode, string DepartmentCode, string RoomCode)>
        SeedEligibleMemberAsync(IServiceProvider services, string prefix)
    {
        ICreateDepartmentUseCase createDept = services.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = services.GetRequiredService<ICreateRoomUseCase>();
        IRegisterWorkforceMemberUseCase registerMember = services.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        ICreateWorkAssignmentUseCase createAssignment = services.GetRequiredService<ICreateWorkAssignmentUseCase>();
        IListWorkforceMembersUseCase listMembers = services.GetRequiredService<IListWorkforceMembersUseCase>();

        string dept = $"{prefix}-dept";
        string workAssignmentCode = $"{prefix}-wa-1";

        await createDept.ExecuteAsync(dept, CancellationToken.None).ConfigureAwait(true);
        string roomCode = await createRoom.ExecuteAsync($"{prefix}-101", "Lab", CancellationToken.None)
            .ConfigureAwait(true);

        string memberCode = await registerMember.ExecuteAsync(
                "Ada",
                "Lovelace",
                UniqueUin(prefix, 1),
                "Employee",
                dept,
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<WorkforceMemberListItem> members = await listMembers.ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        string partyCode = members.Single(item => item.WorkforceMemberCode == memberCode).PartyCode;

        await createAssignment.ExecuteAsync(workAssignmentCode, memberCode, roomCode, isPrimary: true, CancellationToken.None)
            .ConfigureAwait(true);

        return (memberCode, partyCode, dept, roomCode);
    }

    private static string UniqueUin(string prefix, int salt)
    {
        int hash = Math.Abs(HashCode.Combine(prefix, salt)) % 1_000_000_000;
        return hash.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
    }
}
