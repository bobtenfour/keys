using KeyInventory.Application.Lookup;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

public sealed class AdminMaintenanceWorkflowTests : IAsyncLifetime
{
    private ServiceProvider? _services;

    public async Task InitializeAsync()
    {
        string connectionString = KeyInventorySqlServerTestConnection.RequireIsolatedDatabase();
        ServiceCollection services = new();
        LoanVerticalComposition.AddLoanVertical(services, connectionString);
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(true);
    }

    public async Task DisposeAsync()
    {
        if (_services is null)
        {
            return;
        }

        using (IServiceScope scope = _services.CreateScope())
        {
            KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
            await db.Database.EnsureDeletedAsync().ConfigureAwait(true);
        }

        await _services.DisposeAsync().ConfigureAwait(true);
        _services = null;
    }

    [Fact]
    public async Task ActivateAndRetireDepartmentAndRoom()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IRetireDepartmentUseCase retireDept = scope.ServiceProvider.GetRequiredService<IRetireDepartmentUseCase>();
        IActivateDepartmentUseCase activateDept = scope.ServiceProvider.GetRequiredService<IActivateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IRetireRoomUseCase retireRoom = scope.ServiceProvider.GetRequiredService<IRetireRoomUseCase>();
        IActivateRoomUseCase activateRoom = scope.ServiceProvider.GetRequiredService<IActivateRoomUseCase>();
        IListDepartmentsUseCase listDepts = scope.ServiceProvider.GetRequiredService<IListDepartmentsUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("am-dept", CancellationToken.None).ConfigureAwait(true);
        Guid amDeptId = (await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "am-dept").DepartmentId;
        string roomCode = await createRoom.ExecuteAsync("am-dept", "101", "Lab", CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "AM-KEY-1", "01", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        await retireDept.ExecuteAsync(amDeptId, CancellationToken.None).ConfigureAwait(true);
        Assert.False((await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "am-dept").IsActive);
        await activateDept.ExecuteAsync(amDeptId, CancellationToken.None).ConfigureAwait(true);
        Assert.True((await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "am-dept").IsActive);

        await retireRoom.ExecuteAsync(roomCode, CancellationToken.None).ConfigureAwait(true);
        IListRoomsUseCase listRooms = scope.ServiceProvider.GetRequiredService<IListRoomsUseCase>();
        Assert.False((await listRooms.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == roomCode).IsActive);
        await activateRoom.ExecuteAsync(roomCode, CancellationToken.None).ConfigureAwait(true);
        Assert.True((await listRooms.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == roomCode).IsActive);

        Assert.Equal(1, await db.Departments.CountAsync(item => item.DepartmentCode == "am-dept").ConfigureAwait(true));
        Assert.Equal(1, await db.Rooms.CountAsync(item => item.RoomCode == roomCode).ConfigureAwait(true));
        Assert.Equal(1, await db.KeyAssets.CountAsync(item => item.KeyNumber == "AM-KEY-1").ConfigureAwait(true));
    }


    [Fact]
    public async Task RoomNumberPartyNameUinAndDepartmentUpdatesPersist()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IUpdateWorkforceMemberDepartmentUseCase updateDepartment =
            scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberDepartmentUseCase>();
        IUpdateWorkforceMemberWorkforceTypeUseCase updateType =
            scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberWorkforceTypeUseCase>();
        IUpdateRoomNumberUseCase updateRoomNumber = scope.ServiceProvider.GetRequiredService<IUpdateRoomNumberUseCase>();
        IUpdatePartyNameUseCase updatePartyName = scope.ServiceProvider.GetRequiredService<IUpdatePartyNameUseCase>();
        ICorrectPartyUinUseCase correctUin = scope.ServiceProvider.GetRequiredService<ICorrectPartyUinUseCase>();
        IRegisterWorkforceMemberUseCase registerMember =
            scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        IListWorkforceMembersUseCase listMembers =
            scope.ServiceProvider.GetRequiredService<IListWorkforceMembersUseCase>();
        IListRoomsUseCase listRooms = scope.ServiceProvider.GetRequiredService<IListRoomsUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-wm")
            .ConfigureAwait(true);
        await createDept.ExecuteAsync("am-wm-dept-2", CancellationToken.None).ConfigureAwait(true);

        IEndWorkAssignmentUseCase endAssignment = scope.ServiceProvider.GetRequiredService<IEndWorkAssignmentUseCase>();
        IListWorkAssignmentsUseCase listAssignments =
            scope.ServiceProvider.GetRequiredService<IListWorkAssignmentsUseCase>();
        Guid seededAssignmentId = (await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.IsActive && item.WorkforceMemberCode == seeded.MemberCode)
            .WorkAssignmentId;
        await endAssignment.ExecuteAsync(seededAssignmentId, CancellationToken.None).ConfigureAwait(true);

        await updateDepartment.ExecuteAsync(seeded.MemberCode, "am-wm-dept-2", CancellationToken.None)
            .ConfigureAwait(true);
        await updateType.ExecuteAsync(seeded.MemberCode, "Contractor", CancellationToken.None).ConfigureAwait(true);
        await updateRoomNumber.ExecuteAsync(seeded.RoomCode, "201", CancellationToken.None).ConfigureAwait(true);
        await updatePartyName.ExecuteAsync(seeded.PartyCode, "Augusta", "King", CancellationToken.None)
            .ConfigureAwait(true);
        string newUin = UniqueUin("am-wm-new", 7);
        await correctUin.ExecuteAsync(seeded.PartyCode, newUin, CancellationToken.None).ConfigureAwait(true);

        WorkforceMemberListItem updated = (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal("am-wm-dept-2", updated.DepartmentCode);
        Assert.Equal(nameof(WorkforceType.Contractor), updated.WorkforceType);
        Assert.Equal("Augusta", updated.FirstName);
        Assert.Equal("King", updated.LastName);
        Assert.Equal(newUin, updated.Uin);

        RoomListItem room = (await listRooms.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == seeded.RoomCode);
        Assert.Equal("201", room.RoomNumber);

        string collisionUin = UniqueUin("am-wm-collision", 8);
        await registerMember.ExecuteAsync(
                "Grace",
                "Hopper",
                collisionUin,
                "Employee",
                "am-wm-dept-2",
                CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                correctUin.ExecuteAsync(seeded.PartyCode, collisionUin, CancellationToken.None))
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task WorkforceMemberRelationshipUpdatesAndTerminateRemainValid()
    {
        using IServiceScope scope = CreateScope();
        IUpdateWorkforceMemberDepartmentUseCase updateDepartment =
            scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberDepartmentUseCase>();
        IUpdateWorkforceMemberWorkforceTypeUseCase updateType =
            scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberWorkforceTypeUseCase>();
        ITerminateWorkforceMemberUseCase terminate =
            scope.ServiceProvider.GetRequiredService<ITerminateWorkforceMemberUseCase>();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IListOutstandingReturnObligationsUseCase obligations =
            scope.ServiceProvider.GetRequiredService<IListOutstandingReturnObligationsUseCase>();
        IListWorkforceMembersUseCase listMembers =
            scope.ServiceProvider.GetRequiredService<IListWorkforceMembersUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-term")
            .ConfigureAwait(true);
        await createDept.ExecuteAsync("am-term-dept-2", CancellationToken.None).ConfigureAwait(true);

        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        ICreateWorkAssignmentUseCase createAssignment =
            scope.ServiceProvider.GetRequiredService<ICreateWorkAssignmentUseCase>();
        IEndWorkAssignmentUseCase endAssignment = scope.ServiceProvider.GetRequiredService<IEndWorkAssignmentUseCase>();
        IListWorkAssignmentsUseCase listAssignments =
            scope.ServiceProvider.GetRequiredService<IListWorkAssignmentsUseCase>();
        string newRoom = await createRoom.ExecuteAsync("am-term-dept-2", "301", "Moved", CancellationToken.None)
            .ConfigureAwait(true);
        await endAssignment.ExecuteAsync(
                (await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
                    .Single(item => item.IsActive && item.WorkforceMemberCode == seeded.MemberCode)
                    .WorkAssignmentId,
                CancellationToken.None)
            .ConfigureAwait(true);

        await updateDepartment.ExecuteAsync(seeded.MemberCode, "am-term-dept-2", CancellationToken.None)
            .ConfigureAwait(true);
        await createAssignment.ExecuteAsync(seeded.MemberCode, newRoom, CancellationToken.None)
            .ConfigureAwait(true);
        await updateType.ExecuteAsync(seeded.MemberCode, "Contractor", CancellationToken.None).ConfigureAwait(true);

        WorkforceMemberListItem updated = (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal("am-term-dept-2", updated.DepartmentCode);
        Assert.Equal(nameof(WorkforceType.Contractor), updated.WorkforceType);
        Assert.Equal(seeded.PartyCode, updated.PartyCode);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "am-term-key", "01", KeyAccessClassification.Regular).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 9, 18, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-am-term",
                "am-term-key",
                "01",
                seeded.MemberCode,
                "Department",
                "am-term-dept-2",
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        await terminate.ExecuteAsync(seeded.MemberCode, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(
            nameof(WorkforceMemberStatus.Terminated),
            (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
                .Single(item => item.WorkforceMemberCode == seeded.MemberCode).Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateDepartment.ExecuteAsync(seeded.MemberCode, "am-term-dept-2", CancellationToken.None))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateType.ExecuteAsync(seeded.MemberCode, "Employee", CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<OutstandingReturnObligationItem> outstanding = await obligations
            .ExecuteAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(outstanding, item => item.LoanCode == "loan-am-term");

        Assert.Equal(1, await db.Loans.CountAsync(item => item.LoanCode == "loan-am-term").ConfigureAwait(true));
        Assert.Equal(
            nameof(KeyInventory.Domain.Loans.LoanStatus.Open),
            (await db.Loans.SingleAsync(item => item.LoanCode == "loan-am-term").ConfigureAwait(true)).Status);
    }

    [Fact]
    public async Task WorkAssignmentEndPersistsWithoutDelete()
    {
        using IServiceScope scope = CreateScope();
        ICreateWorkAssignmentUseCase createAssignment =
            scope.ServiceProvider.GetRequiredService<ICreateWorkAssignmentUseCase>();
        IEndWorkAssignmentUseCase endAssignment = scope.ServiceProvider.GetRequiredService<IEndWorkAssignmentUseCase>();
        IListWorkAssignmentsUseCase listAssignments =
            scope.ServiceProvider.GetRequiredService<IListWorkAssignmentsUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-wa")
            .ConfigureAwait(true);
        string secondRoom = await createRoom.ExecuteAsync(seeded.DepartmentCode, "202", "Office", CancellationToken.None).ConfigureAwait(true);
        await createAssignment.ExecuteAsync(seeded.MemberCode, secondRoom, CancellationToken.None)
            .ConfigureAwait(true);

        WorkAssignmentListItem second = (await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.IsActive
                && item.WorkforceMemberCode == seeded.MemberCode
                && item.RoomCode == secondRoom);

        await endAssignment.ExecuteAsync(second.WorkAssignmentId, CancellationToken.None).ConfigureAwait(true);
        WorkAssignmentListItem ended = (await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkAssignmentId == second.WorkAssignmentId);
        Assert.False(ended.IsActive);
        Assert.Equal(1, await db.WorkAssignments.CountAsync(item => item.WorkAssignmentId == second.WorkAssignmentId)
            .ConfigureAwait(true));
    }

    [Fact]
    public async Task IssueReceiveLookupAndReportsRemainValidAfterMaintenance()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();
        IOperationalReportsUseCase reports = scope.ServiceProvider.GetRequiredService<IOperationalReportsUseCase>();
        IRetireDepartmentUseCase retireDept = scope.ServiceProvider.GetRequiredService<IRetireDepartmentUseCase>();
        IActivateDepartmentUseCase activateDept = scope.ServiceProvider.GetRequiredService<IActivateDepartmentUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-flow")
            .ConfigureAwait(true);
        Guid flowDeptId = (await scope.ServiceProvider.GetRequiredService<IListDepartmentsUseCase>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == seeded.DepartmentCode).DepartmentId;
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "am-flow-key", "01", KeyAccessClassification.Regular).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 9, 19, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-am-flow",
                "am-flow-key",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        await retireDept.ExecuteAsync(flowDeptId, CancellationToken.None).ConfigureAwait(true);
        await activateDept.ExecuteAsync(flowDeptId, CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> found = await lookup.SearchKeysAsync("am-flow-key", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(found);
        Assert.Equal(OperationalKeyAvailability.Issued, found[0].AvailabilityStatus);

        IReadOnlyList<KeyCatalogReportRow> catalog = await reports
            .ListKeyCatalogReportAsync("am-flow-key", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(catalog, row => row.KeyNumber == "am-flow-key" && row.MedecoKeyCode == "01");

        await completeReturn.ExecuteAsync("return-am-flow", "loan-am-flow", issued.AddHours(1), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(
            OperationalKeyAvailability.Available,
            (await lookup.SearchKeysAsync("am-flow-key", CancellationToken.None).ConfigureAwait(true))[0]
                .AvailabilityStatus);
    }

    private static string UniqueUin(string prefix, int salt)
    {
        int hash = Math.Abs(HashCode.Combine(prefix, salt)) % 1_000_000_000;
        return hash.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
