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
    public async Task ActivateAndRetireDepartmentRoomAndKeyType()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IRetireDepartmentUseCase retireDept = scope.ServiceProvider.GetRequiredService<IRetireDepartmentUseCase>();
        IActivateDepartmentUseCase activateDept = scope.ServiceProvider.GetRequiredService<IActivateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IRetireRoomUseCase retireRoom = scope.ServiceProvider.GetRequiredService<IRetireRoomUseCase>();
        IActivateRoomUseCase activateRoom = scope.ServiceProvider.GetRequiredService<IActivateRoomUseCase>();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IRetireKeyTypeUseCase retireKeyType = scope.ServiceProvider.GetRequiredService<IRetireKeyTypeUseCase>();
        IActivateKeyTypeUseCase activateKeyType = scope.ServiceProvider.GetRequiredService<IActivateKeyTypeUseCase>();
        IListDepartmentsUseCase listDepts = scope.ServiceProvider.GetRequiredService<IListDepartmentsUseCase>();
        IListKeyTypesUseCase listTypes = scope.ServiceProvider.GetRequiredService<IListKeyTypesUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("am-dept", CancellationToken.None).ConfigureAwait(true);
        Guid amDeptId = (await listDepts.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "am-dept").DepartmentId;
        string roomCode = await createRoom.ExecuteAsync("101", "Lab", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("AM-KEY-1", "01", "am-type", CancellationToken.None).ConfigureAwait(true);

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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                retireKeyType.ExecuteAsync("am-type", CancellationToken.None))
            .ConfigureAwait(true);
        KeyAccessPatternEntity pattern = await db.KeyAccessPatterns.SingleAsync(item => item.KeyNumber == "AM-KEY-1")
            .ConfigureAwait(true);
        pattern.IsActive = false;
        KeyAssetEntity key = await db.KeyAssets.SingleAsync(item => item.KeyNumber == "AM-KEY-1" && item.MedecoKeyCode == "01")
            .ConfigureAwait(true);
        key.IsActive = false;
        await db.SaveChangesAsync().ConfigureAwait(true);
        await retireKeyType.ExecuteAsync("am-type", CancellationToken.None).ConfigureAwait(true);
        Assert.False((await listTypes.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.TypeCode == "am-type").IsActive);
        await activateKeyType.ExecuteAsync("am-type", CancellationToken.None).ConfigureAwait(true);
        Assert.True((await listTypes.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.TypeCode == "am-type").IsActive);

        Assert.Equal(1, await db.Departments.CountAsync(item => item.DepartmentCode == "am-dept").ConfigureAwait(true));
        Assert.Equal(1, await db.Rooms.CountAsync(item => item.RoomCode == roomCode).ConfigureAwait(true));
        Assert.Equal(1, await db.KeyTypes.CountAsync(item => item.TypeCode == "am-type").ConfigureAwait(true));
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

        await updateDepartment.ExecuteAsync(seeded.MemberCode, "am-term-dept-2", CancellationToken.None)
            .ConfigureAwait(true);
        await updateType.ExecuteAsync(seeded.MemberCode, "Contractor", CancellationToken.None).ConfigureAwait(true);

        WorkforceMemberListItem updated = (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal("am-term-dept-2", updated.DepartmentCode);
        Assert.Equal(nameof(WorkforceType.Contractor), updated.WorkforceType);
        Assert.Equal(seeded.PartyCode, updated.PartyCode);

        await createKey.ExecuteAsync("am-term-key", "01", "mechanical", CancellationToken.None).ConfigureAwait(true);
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
    public async Task WorkAssignmentEndAndPrimaryMaintenancePersistWithoutDelete()
    {
        using IServiceScope scope = CreateScope();
        ICreateWorkAssignmentUseCase createAssignment =
            scope.ServiceProvider.GetRequiredService<ICreateWorkAssignmentUseCase>();
        IEndWorkAssignmentUseCase endAssignment = scope.ServiceProvider.GetRequiredService<IEndWorkAssignmentUseCase>();
        IMarkWorkAssignmentPrimaryUseCase markPrimary =
            scope.ServiceProvider.GetRequiredService<IMarkWorkAssignmentPrimaryUseCase>();
        IClearWorkAssignmentPrimaryUseCase clearPrimary =
            scope.ServiceProvider.GetRequiredService<IClearWorkAssignmentPrimaryUseCase>();
        IListWorkAssignmentsUseCase listAssignments =
            scope.ServiceProvider.GetRequiredService<IListWorkAssignmentsUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-wa")
            .ConfigureAwait(true);
        const string originalPrimary = "am-wa-wa-1";
        string secondRoom = await createRoom.ExecuteAsync("202", "Office", CancellationToken.None).ConfigureAwait(true);
        await createAssignment.ExecuteAsync("am-wa-2", seeded.MemberCode, secondRoom, isPrimary: false, CancellationToken.None)
            .ConfigureAwait(true);

        await markPrimary.ExecuteAsync("am-wa-2", CancellationToken.None).ConfigureAwait(true);
        IReadOnlyList<WorkAssignmentListItem> afterMark = await listAssignments.ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.True(afterMark.Single(item => item.WorkAssignmentCode == "am-wa-2").IsPrimary);
        Assert.False(afterMark.Single(item => item.WorkAssignmentCode == originalPrimary).IsPrimary);

        await clearPrimary.ExecuteAsync("am-wa-2", CancellationToken.None).ConfigureAwait(true);
        Assert.False((await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkAssignmentCode == "am-wa-2").IsPrimary);

        await endAssignment.ExecuteAsync("am-wa-2", CancellationToken.None).ConfigureAwait(true);
        WorkAssignmentListItem ended = (await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkAssignmentCode == "am-wa-2");
        Assert.False(ended.IsActive);
        Assert.False(ended.IsPrimary);
        Assert.Equal(1, await db.WorkAssignments.CountAsync(item => item.WorkAssignmentCode == "am-wa-2")
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
        await createKey.ExecuteAsync("am-flow-key", "01", "mechanical", CancellationToken.None).ConfigureAwait(true);

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
