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
    public async Task ActivateAndRetireOrganizationDepartmentBuildingRoomAndKeyType()
    {
        using IServiceScope scope = CreateScope();
        ICreateOrganizationUseCase createOrg = scope.ServiceProvider.GetRequiredService<ICreateOrganizationUseCase>();
        IRetireOrganizationUseCase retireOrg = scope.ServiceProvider.GetRequiredService<IRetireOrganizationUseCase>();
        IActivateOrganizationUseCase activateOrg = scope.ServiceProvider.GetRequiredService<IActivateOrganizationUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IRetireDepartmentUseCase retireDept = scope.ServiceProvider.GetRequiredService<IRetireDepartmentUseCase>();
        IActivateDepartmentUseCase activateDept = scope.ServiceProvider.GetRequiredService<IActivateDepartmentUseCase>();
        ICreateBuildingUseCase createBuilding = scope.ServiceProvider.GetRequiredService<ICreateBuildingUseCase>();
        IRetireBuildingUseCase retireBuilding = scope.ServiceProvider.GetRequiredService<IRetireBuildingUseCase>();
        IActivateBuildingUseCase activateBuilding = scope.ServiceProvider.GetRequiredService<IActivateBuildingUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IRetireRoomUseCase retireRoom = scope.ServiceProvider.GetRequiredService<IRetireRoomUseCase>();
        IActivateRoomUseCase activateRoom = scope.ServiceProvider.GetRequiredService<IActivateRoomUseCase>();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IRetireKeyTypeUseCase retireKeyType = scope.ServiceProvider.GetRequiredService<IRetireKeyTypeUseCase>();
        IActivateKeyTypeUseCase activateKeyType = scope.ServiceProvider.GetRequiredService<IActivateKeyTypeUseCase>();
        IListOrganizationsUseCase listOrgs = scope.ServiceProvider.GetRequiredService<IListOrganizationsUseCase>();
        IListKeyTypesUseCase listTypes = scope.ServiceProvider.GetRequiredService<IListKeyTypesUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createOrg.ExecuteAsync("am-org", CancellationToken.None).ConfigureAwait(true);
        await createDept.ExecuteAsync("am-org", "am-dept", CancellationToken.None).ConfigureAwait(true);
        await createBuilding.ExecuteAsync("am-bldg", CancellationToken.None).ConfigureAwait(true);
        await createRoom.ExecuteAsync("am-room", "am-bldg", "101", "Lab", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("AM-KEY-1", "am-type", CancellationToken.None).ConfigureAwait(true);

        await retireOrg.ExecuteAsync("am-org", CancellationToken.None).ConfigureAwait(true);
        Assert.False((await listOrgs.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.OrganizationCode == "am-org").IsActive);
        await activateOrg.ExecuteAsync("am-org", CancellationToken.None).ConfigureAwait(true);
        Assert.True((await listOrgs.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.OrganizationCode == "am-org").IsActive);

        await retireDept.ExecuteAsync("am-org", "am-dept", CancellationToken.None).ConfigureAwait(true);
        await retireOrg.ExecuteAsync("am-org", CancellationToken.None).ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                activateDept.ExecuteAsync("am-org", "am-dept", CancellationToken.None))
            .ConfigureAwait(true);
        await activateOrg.ExecuteAsync("am-org", CancellationToken.None).ConfigureAwait(true);
        await activateDept.ExecuteAsync("am-org", "am-dept", CancellationToken.None).ConfigureAwait(true);

        await retireRoom.ExecuteAsync("am-room", CancellationToken.None).ConfigureAwait(true);
        await retireBuilding.ExecuteAsync("am-bldg", CancellationToken.None).ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                activateRoom.ExecuteAsync("am-room", CancellationToken.None))
            .ConfigureAwait(true);
        await activateBuilding.ExecuteAsync("am-bldg", CancellationToken.None).ConfigureAwait(true);
        await activateRoom.ExecuteAsync("am-room", CancellationToken.None).ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                retireKeyType.ExecuteAsync("am-type", CancellationToken.None))
            .ConfigureAwait(true);
        KeyAssetEntity key = await db.KeyAssets.SingleAsync(item => item.CatalogKeyCode == "AM-KEY-1")
            .ConfigureAwait(true);
        key.IsActive = false;
        await db.SaveChangesAsync().ConfigureAwait(true);
        await retireKeyType.ExecuteAsync("am-type", CancellationToken.None).ConfigureAwait(true);
        Assert.False((await listTypes.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.TypeCode == "am-type").IsActive);
        await activateKeyType.ExecuteAsync("am-type", CancellationToken.None).ConfigureAwait(true);
        Assert.True((await listTypes.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.TypeCode == "am-type").IsActive);

        Assert.Equal(1, await db.Organizations.CountAsync(item => item.OrganizationCode == "am-org").ConfigureAwait(true));
        Assert.Equal(1, await db.Rooms.CountAsync(item => item.RoomCode == "am-room").ConfigureAwait(true));
        Assert.Equal(1, await db.KeyTypes.CountAsync(item => item.TypeCode == "am-type").ConfigureAwait(true));
    }

    [Fact]
    public async Task WorkforceMemberRelationshipUpdatesAndTerminateRemainValid()
    {
        using IServiceScope scope = CreateScope();
        ICreateOrganizationUseCase createOrg = scope.ServiceProvider.GetRequiredService<ICreateOrganizationUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IUpdateWorkforceMemberOrganizationDepartmentUseCase updateOrgDept =
            scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberOrganizationDepartmentUseCase>();
        IUpdateWorkforceMemberResponsibleManagerUseCase updateManager =
            scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberResponsibleManagerUseCase>();
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
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-wm")
            .ConfigureAwait(true);
        await createOrg.ExecuteAsync("am-wm-org-2", CancellationToken.None).ConfigureAwait(true);
        await createDept.ExecuteAsync("am-wm-org-2", "am-wm-dept-2", CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<WorkforceMemberListItem> members = await listMembers.ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        string managerCode = members.First(item =>
            item.Status == nameof(WorkforceMemberStatus.Active)
            && item.WorkforceMemberCode != seeded.MemberCode).WorkforceMemberCode;

        await updateOrgDept.ExecuteAsync(seeded.MemberCode, "am-wm-org-2", "am-wm-dept-2", CancellationToken.None)
            .ConfigureAwait(true);
        await updateManager.ExecuteAsync(seeded.MemberCode, managerCode, CancellationToken.None).ConfigureAwait(true);
        await updateType.ExecuteAsync(seeded.MemberCode, "Contractor", CancellationToken.None).ConfigureAwait(true);

        WorkforceMemberListItem updated = (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal("am-wm-org-2", updated.OrganizationCode);
        Assert.Equal("am-wm-dept-2", updated.DepartmentCode);
        Assert.Equal(managerCode, updated.ResponsibleManagerWorkforceMemberCode);
        Assert.Equal(nameof(WorkforceType.Contractor), updated.WorkforceType);
        Assert.Equal(seeded.PartyCode, updated.PartyCode);

        await createKey.ExecuteAsync("am-wm-key", "mechanical", CancellationToken.None).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 9, 18, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-am-wm",
                "am-wm-key",
                seeded.MemberCode,
                "Department",
                "am-wm-dept-2",
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
                updateOrgDept.ExecuteAsync(seeded.MemberCode, "am-wm-org-2", "am-wm-dept-2", CancellationToken.None))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateType.ExecuteAsync(seeded.MemberCode, "Employee", CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<OutstandingReturnObligationItem> outstanding = await obligations
            .ExecuteAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(outstanding, item => item.LoanCode == "loan-am-wm");

        Assert.Equal(1, await db.Loans.CountAsync(item => item.LoanCode == "loan-am-wm").ConfigureAwait(true));
        Assert.Equal(
            nameof(KeyInventory.Domain.Loans.LoanStatus.Open),
            (await db.Loans.SingleAsync(item => item.LoanCode == "loan-am-wm").ConfigureAwait(true)).Status);
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
        const string secondRoom = "am-wa-room-2";
        await createRoom.ExecuteAsync(secondRoom, "am-wa-bldg", "202", "Office", CancellationToken.None)
            .ConfigureAwait(true);
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
        IRetireOrganizationUseCase retireOrg = scope.ServiceProvider.GetRequiredService<IRetireOrganizationUseCase>();
        IActivateOrganizationUseCase activateOrg = scope.ServiceProvider.GetRequiredService<IActivateOrganizationUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "am-flow")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("am-flow-key", "mechanical", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 9, 19, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-am-flow",
                "am-flow-key",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        await retireOrg.ExecuteAsync("am-flow-org", CancellationToken.None).ConfigureAwait(true);
        await activateOrg.ExecuteAsync("am-flow-org", CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> found = await lookup.SearchKeysAsync("am-flow-key", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(found);
        Assert.Equal(OperationalKeyAvailability.Issued, found[0].AvailabilityStatus);

        IReadOnlyList<KeyCatalogReportRow> catalog = await reports
            .ListKeyCatalogReportAsync("am-flow-key", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(catalog, row => row.CatalogKeyCode == "am-flow-key");

        await completeReturn.ExecuteAsync("return-am-flow", "loan-am-flow", issued.AddHours(1), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(
            OperationalKeyAvailability.Available,
            (await lookup.SearchKeysAsync("am-flow-key", CancellationToken.None).ConfigureAwait(true))[0]
                .AvailabilityStatus);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
