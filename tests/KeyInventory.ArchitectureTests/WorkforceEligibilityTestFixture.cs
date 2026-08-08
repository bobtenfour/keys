using KeyInventory.Application.Workforce;
using Microsoft.Extensions.DependencyInjection;

namespace KeyInventory.ArchitectureTests;

internal static class WorkforceEligibilityTestFixture
{
    internal static async Task<(string MemberCode, string PartyCode, string DepartmentCode, string RoomCode)>
        SeedEligibleMemberAsync(IServiceProvider services, string prefix)
    {
        ICreateOrganizationUseCase createOrg = services.GetRequiredService<ICreateOrganizationUseCase>();
        ICreateDepartmentUseCase createDept = services.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateBuildingUseCase createBuilding = services.GetRequiredService<ICreateBuildingUseCase>();
        ICreateRoomUseCase createRoom = services.GetRequiredService<ICreateRoomUseCase>();
        ICreatePartyUseCase createParty = services.GetRequiredService<ICreatePartyUseCase>();
        ICreateBootstrapWorkforcePairUseCase createPair = services.GetRequiredService<ICreateBootstrapWorkforcePairUseCase>();
        ICreateWorkforceMemberUseCase createMember = services.GetRequiredService<ICreateWorkforceMemberUseCase>();
        ICreateWorkAssignmentUseCase createAssignment = services.GetRequiredService<ICreateWorkAssignmentUseCase>();
        IListWorkforceMembersUseCase listMembers = services.GetRequiredService<IListWorkforceMembersUseCase>();

        string org = $"{prefix}-org";
        string dept = $"{prefix}-dept";
        string building = $"{prefix}-bldg";
        string room = $"{prefix}-room";
        string party1 = $"{prefix}-party-1";
        string member1 = $"{prefix}-wm-1";

        await createOrg.ExecuteAsync(org, CancellationToken.None).ConfigureAwait(true);
        await createDept.ExecuteAsync(org, dept, CancellationToken.None).ConfigureAwait(true);
        await createBuilding.ExecuteAsync(building, CancellationToken.None).ConfigureAwait(true);
        await createRoom.ExecuteAsync(room, building, $"{prefix}-101", "Lab", CancellationToken.None).ConfigureAwait(true);
        await createParty.ExecuteAsync(party1, "Ada", "Lovelace", UniqueUin(prefix, 1), CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<WorkforceMemberListItem> existing = await listMembers.ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        if (existing.Count == 0)
        {
            string party2 = $"{prefix}-party-2";
            string member2 = $"{prefix}-wm-2";
            await createParty.ExecuteAsync(party2, "Alan", "Turing", UniqueUin(prefix, 2), CancellationToken.None)
                .ConfigureAwait(true);
            await createPair.ExecuteAsync(
                    member1,
                    party1,
                    "Employee",
                    member2,
                    party2,
                    "Employee",
                    org,
                    dept,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }
        else
        {
            string managerCode = existing.First(item => string.Equals(item.Status, "Active", StringComparison.Ordinal))
                .WorkforceMemberCode;
            await createMember.ExecuteAsync(
                    member1,
                    party1,
                    "Employee",
                    org,
                    dept,
                    managerCode,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }

        await createAssignment.ExecuteAsync($"{prefix}-wa-1", member1, room, isPrimary: true, CancellationToken.None)
            .ConfigureAwait(true);

        return (member1, party1, dept, room);
    }

    private static string UniqueUin(string prefix, int salt)
    {
        int hash = Math.Abs(HashCode.Combine(prefix, salt)) % 1_000_000_000;
        return hash.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
    }
}
