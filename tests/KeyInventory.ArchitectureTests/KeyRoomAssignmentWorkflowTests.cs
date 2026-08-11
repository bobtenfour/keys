using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class KeyRoomAssignmentWorkflowTests : IAsyncLifetime
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
    public async Task AssignRemoveAndLookupReuseAuthoritativeRoomAssignments()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IKeyRoomAssignmentUseCase assignments = scope.ServiceProvider.GetRequiredService<IKeyRoomAssignmentUseCase>();
        IListKeyAssetsUseCase listKeys = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();
        IOperationalReportsUseCase reports = scope.ServiceProvider.GetRequiredService<IOperationalReportsUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        string roomCode1 = await createRoom.ExecuteAsync("101", "Office", CancellationToken.None).ConfigureAwait(true);
        string roomCode2 = await createRoom.ExecuteAsync("102", "Lab", CancellationToken.None).ConfigureAwait(true);

        await createKey.ExecuteAsync("KRA-KEY-1", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("KRA-KEY-2", "mechanical", CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyOpenedRoomItem> zero = await assignments
            .ListOpenedRoomsAsync("KRA-KEY-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Empty(zero);

        await assignments.AssignRoomAsync("KRA-KEY-1", roomCode1, CancellationToken.None).ConfigureAwait(true);
        await assignments.AssignRoomAsync("KRA-KEY-1", roomCode2, CancellationToken.None).ConfigureAwait(true);
        await assignments.AssignRoomAsync("KRA-KEY-2", roomCode1, CancellationToken.None).ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                assignments.AssignRoomAsync("KRA-KEY-1", roomCode1, CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<KeyOpenedRoomItem> forKey1 = await assignments
            .ListOpenedRoomsAsync("KRA-KEY-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(2, forKey1.Count);
        Assert.Contains(forKey1, room => room.RoomCode == roomCode1 && room.RoomNumber == "101");
        Assert.Contains(forKey1, room => room.RoomCode == roomCode2 && room.RoomNumber == "102");

        IReadOnlyList<KeyAssetListItem> catalog = await listKeys.ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        KeyAssetListItem catalogKey1 = Assert.Single(catalog, item => item.CatalogKeyCode == "KRA-KEY-1");
        Assert.Equal(2, catalogKey1.OpenedRooms.Count);
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.Format(forKey1),
            KeyOpenedRoomDisplayFormatter.Format(catalogKey1.OpenedRooms));

        IReadOnlyList<KeyLookupResult> found = await lookup.SearchKeysAsync("KRA-KEY-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(found);
        Assert.Equal(2, found[0].OpenedRooms.Count);
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.Format(forKey1),
            KeyOpenedRoomDisplayFormatter.Format(found[0].OpenedRooms));

        IReadOnlyList<KeyCatalogReportRow> report = await reports
            .ListKeyCatalogReportAsync("KRA-KEY", CancellationToken.None)
            .ConfigureAwait(true);
        KeyCatalogReportRow reportKey1 = Assert.Single(report, row => row.CatalogKeyCode == "KRA-KEY-1");
        Assert.Equal(2, reportKey1.OpenedRooms.Count);
        string reportCsv = reports.FormatKeyCatalogCsv(report);
        string roomsDisplay = KeyOpenedRoomDisplayFormatter.Format(reportKey1.OpenedRooms);
        Assert.Contains("Rooms Opened", reportCsv, StringComparison.Ordinal);
        Assert.Contains(roomsDisplay, reportCsv, StringComparison.Ordinal);
        Assert.Equal(reportCsv, reports.FormatKeyCatalogCsv(report));

        await assignments.RemoveRoomAsync("KRA-KEY-1", roomCode2, CancellationToken.None).ConfigureAwait(true);
        IReadOnlyList<KeyOpenedRoomItem> afterRemove = await assignments
            .ListOpenedRoomsAsync("KRA-KEY-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(afterRemove);
        Assert.Equal(roomCode1, afterRemove[0].RoomCode);

        Assert.Null(typeof(KeyAssetEntity).GetProperty("BuildingCode"));
        Assert.Null(typeof(KeyAssetEntity).GetProperty("Building"));
        Assert.DoesNotContain(
            db.Model.FindEntityType(typeof(KeyRoomAssignmentEntity))!.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType.Name.Contains("Lock", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExistingIssueAndReceiveRemainValidWithRoomAssignments()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IKeyRoomAssignmentUseCase assignments = scope.ServiceProvider.GetRequiredService<IKeyRoomAssignmentUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "kra-flow")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("kra-flow-key", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await assignments.AssignRoomAsync("kra-flow-key", seeded.RoomCode, CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 9, 16, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-kra-flow",
                "kra-flow-key",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Contains(
            await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true),
            loan => loan.LoanCode == "loan-kra-flow");

        await completeReturn.ExecuteAsync("return-kra-flow", "loan-kra-flow", issued.AddHours(2), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.DoesNotContain(
            await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true),
            loan => loan.LoanCode == "loan-kra-flow");
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
