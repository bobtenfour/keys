using System.Reflection;
using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KeyInventory.Domain.Catalog;

namespace KeyInventory.ArchitectureTests;

public sealed class KeyAccessCopy1WorkflowTests : IAsyncLifetime
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
    public async Task ManyCopiesPerKeyNumberWithMedecoUniquenessAndCrossKeyMedecoReuse()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IListKeyAssetsUseCase listKeys = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66800", "27", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66800", "28", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, CancellationToken.None))
            .ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66801", "26", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyAssetListItem> keys = await listKeys.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(3, keys.Count(item => item.KeyNumber == "66800"));
        Assert.Contains(keys, item => item.KeyNumber == "66800" && item.MedecoKeyCode == "26");
        Assert.Contains(keys, item => item.KeyNumber == "66800" && item.MedecoKeyCode == "27");
        Assert.Contains(keys, item => item.KeyNumber == "66800" && item.MedecoKeyCode == "28");
        Assert.Contains(keys, item => item.KeyNumber == "66801" && item.MedecoKeyCode == "26");
        Assert.Equal(4, keys.Select(item => item.KeyAssetId).Distinct().Count());
    }

    [Fact]
    public async Task RegularKeyNumberSingleRoomAndIdenticalDerivedAccessForEveryCopy()
    {
        using IServiceScope scope = CreateScope();
        IKeyAccessResolutionPort access = scope.ServiceProvider.GetRequiredService<IKeyAccessResolutionPort>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IListKeyAssetsUseCase listKeys = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();

        await createDept.ExecuteAsync("kc1-dept", CancellationToken.None).ConfigureAwait(true);
        string roomA = await createRoom.ExecuteAsync("kc1-dept", "410D", "Suite", CancellationToken.None).ConfigureAwait(true);
        string roomB = await createRoom.ExecuteAsync("kc1-dept", "411A", "Lab", CancellationToken.None).ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, roomA, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "27", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66801", "01", KeyAccessClassification.Regular, roomA, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "MASTER1", "01", KeyAccessClassification.Master, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<string> forRoomA = await access
            .ListKeyNumbersOpeningRoomAsync(roomA, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains("66800", forRoomA);
        Assert.Contains("66801", forRoomA);
        Assert.Contains("MASTER1", forRoomA);

        IReadOnlyList<KeyOpenedRoomItem> patternRooms = await access
            .ResolveForKeyNumberAsync("66800", expandMaster: false, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(patternRooms);
        Assert.Equal(roomA, patternRooms[0].RoomCode);

        IReadOnlyList<KeyAssetListItem> copies = (await listKeys.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Where(item => item.KeyNumber == "66800")
            .ToArray();
        Assert.Equal(2, copies.Count);
        Assert.All(copies, copy =>
        {
            Assert.Single(copy.OpenedRooms);
            Assert.Equal(
                KeyOpenedRoomDisplayFormatter.Format(patternRooms),
                KeyOpenedRoomDisplayFormatter.Format(copy.OpenedRooms));
        });

        IReadOnlyList<KeyOpenedRoomItem> masterExpanded = await access
            .ResolveForKeyNumberAsync("MASTER1", expandMaster: true, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(masterExpanded, room => room.RoomCode == roomA);
        Assert.Contains(masterExpanded, room => room.RoomCode == roomB);
    }

    [Fact]
    public async Task SimultaneousIssueOfDifferentCopiesAndOneOpenLoanPerCopy()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "kac-issue")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "66800", "27", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-kac-26",
                "66800",
                "26",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);
        await issue.ExecuteAsync(
                "loan-kac-27",
                "66800",
                "27",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> open = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(open, loan => loan.KeyNumber == "66800" && loan.MedecoKeyCode == "26");
        Assert.Contains(open, loan => loan.KeyNumber == "66800" && loan.MedecoKeyCode == "27");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                issue.ExecuteAsync(
                    "loan-kac-26-dup",
                    "66800",
                    "26",
                    seeded.MemberCode,
                    "Department",
                    seeded.DepartmentCode,
                    issued,
                    issued.AddDays(2),
                    CancellationToken.None))
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task FindAndReportsDistinguishKeyNumberAndMedeco()
    {
        using IServiceScope scope = CreateScope();
        IKeyAccessResolutionPort access = scope.ServiceProvider.GetRequiredService<IKeyAccessResolutionPort>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();
        IOperationalReportsUseCase reports = scope.ServiceProvider.GetRequiredService<IOperationalReportsUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IOperatorAuditTrailUseCase audit = scope.ServiceProvider.GetRequiredService<IOperatorAuditTrailUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "kac-find")
            .ConfigureAwait(true);
        await scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>()
            .ExecuteAsync("kc1-flow-dept", CancellationToken.None).ConfigureAwait(true);
        string roomCode = await createRoom.ExecuteAsync("kc1-flow-dept", "410D", "Office", CancellationToken.None).ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, roomCode, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "27", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 11, 13, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-kac-find",
                "66800",
                "26",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> byKeyNumber = await lookup.SearchKeysAsync("66800", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(2, byKeyNumber.Count);
        Assert.Contains(byKeyNumber, item => item.MedecoKeyCode == "26" && item.AvailabilityStatus == OperationalKeyAvailability.Issued);
        Assert.Contains(byKeyNumber, item => item.MedecoKeyCode == "27" && item.AvailabilityStatus == OperationalKeyAvailability.Available);

        IReadOnlyList<KeyLookupResult> byMedeco = await lookup.SearchKeysAsync("26", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(byMedeco, item => item.KeyNumber == "66800" && item.MedecoKeyCode == "26");

        IReadOnlyList<string> keyNumbersForRoom = await access
            .ListKeyNumbersOpeningRoomAsync(roomCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains("66800", keyNumbersForRoom);

        IReadOnlyList<CurrentKeyHolderReportRow> holders =
            await reports.ListCurrentKeyHoldersAsync("66800", CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(holders, row => row.KeyNumber == "66800" && row.MedecoKeyCode == "26");

        IReadOnlyList<KeyCatalogReportRow> catalog =
            await reports.ListKeyCatalogReportAsync("66800", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(2, catalog.Count);

        IReadOnlyList<OperatorAuditTrailItem> audits = await audit
            .QueryAsync(new OperatorAuditTrailQuery(null, null, null, null, null), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.KeyAccessPatternCreated);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.PhysicalKeyCopyRegistered);
        Assert.Contains(audits, item => item.ActionType == OperatorAuditActions.KeyIssued);
    }

    [Fact]
    public void WebDoesNotReferenceDbContextAndNormalizedOwnershipIsPreserved()
    {
        Assembly webAssembly = typeof(KeyInventory.Web.Program).Assembly;
        string[] dbContextTypes = webAssembly
            .GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => name.Contains("DbContext", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(dbContextTypes);

        Assert.DoesNotContain(
            typeof(KeyInventory.Domain.Catalog.KeyAsset).GetProperties(),
            property => property.Name is "CatalogKeyCode" or "IntendedLock" or "RoomCode" or "OpensAllRooms");
        Assert.Contains(
            typeof(KeyInventory.Domain.Catalog.KeyAsset).GetProperties(),
            property => property.Name == "AccessPattern");
        Assert.Contains(
            typeof(KeyInventory.Domain.Catalog.KeyAccessPattern).GetProperties(),
            property => property.Name == "RoomCode");
        Assert.Contains(
            typeof(KeyInventory.Domain.Catalog.KeyAccessPattern).GetProperties(),
            property => property.Name == "OpensAllRooms");
        Assert.DoesNotContain(
            typeof(KeyInventory.Domain.Catalog.KeyAccessPattern).GetMethods(),
            method => method.Name is "AssignOpenedRoom" or "RemoveOpenedRoom");
        Assert.DoesNotContain(
            typeof(KeyInventory.Domain.Catalog.KeyAsset).GetMethods(),
            method => method.Name == "AssignOpenedRoom");
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
