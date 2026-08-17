using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Reports;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class ClassificationAccessAuthorityTests : IAsyncLifetime
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
    public async Task RegularRequiresOneRoomAndMasterDerivesAllRoomsWithoutJoinTable()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IKeyAccessResolutionPort access = scope.ServiceProvider.GetRequiredService<IKeyAccessResolutionPort>();
        IListKeyAssetsUseCase listKeys = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();
        IOperationalReportsUseCase reports = scope.ServiceProvider.GetRequiredService<IOperationalReportsUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("caa-dept", CancellationToken.None).ConfigureAwait(true);
        string roomCode1 = await createRoom.ExecuteAsync("caa-dept", "101", "Office", CancellationToken.None)
            .ConfigureAwait(true);
        string roomCode2 = await createRoom.ExecuteAsync("caa-dept", "102", "Lab", CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                createKey.RegisterNewKeyAsync(
                    "CAA-REG",
                    "01",
                    KeyAccessClassification.Regular,
                    [],
                    CancellationToken.None))
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                createKey.RegisterNewKeyAsync(
                    "CAA-REG",
                    "01",
                    KeyAccessClassification.Regular,
                    [roomCode1, roomCode2],
                    CancellationToken.None))
            .ConfigureAwait(true);

        await createKey.RegisterNewKeyAsync(
                "CAA-REG",
                "01",
                KeyAccessClassification.Regular,
                [roomCode1],
                CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                createKey.RegisterNewKeyAsync(
                    "CAA-MST",
                    "01",
                    KeyAccessClassification.Master,
                    [roomCode1],
                    CancellationToken.None))
            .ConfigureAwait(true);

        await createKey.RegisterNewKeyAsync(
                "CAA-MST",
                "01",
                KeyAccessClassification.Master,
                [],
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyOpenedRoomItem> regularRooms = await access
            .ResolveForKeyNumberAsync("CAA-REG", expandMaster: false, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(regularRooms);
        Assert.Equal(roomCode1, regularRooms[0].RoomCode);

        IReadOnlyList<KeyOpenedRoomItem> masterDisplay = await access
            .ResolveForKeyNumberAsync("CAA-MST", expandMaster: false, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Empty(masterDisplay);
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.MasterAccessDisplay,
            KeyOpenedRoomDisplayFormatter.FormatAccess(KeyAccessClassification.Master, masterDisplay));

        IReadOnlyList<KeyOpenedRoomItem> masterExpanded = await access
            .ResolveForKeyNumberAsync("CAA-MST", expandMaster: true, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(2, masterExpanded.Count);
        Assert.Contains(masterExpanded, room => room.RoomCode == roomCode1);
        Assert.Contains(masterExpanded, room => room.RoomCode == roomCode2);

        string roomCode3 = await createRoom.ExecuteAsync("caa-dept", "103", "Storage", CancellationToken.None)
            .ConfigureAwait(true);
        IReadOnlyList<KeyOpenedRoomItem> afterAdd = await access
            .ResolveForKeyNumberAsync("CAA-MST", expandMaster: true, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(3, afterAdd.Count);
        Assert.Contains(afterAdd, room => room.RoomCode == roomCode3);

        KeyAssetListItem catalogRegular = (await listKeys.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.KeyNumber == "CAA-REG");
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.FormatAccess(catalogRegular.Classification, catalogRegular.OpenedRooms),
            KeyOpenedRoomDisplayFormatter.Format(regularRooms));

        KeyCatalogReportRow reportMaster = (await reports
                .ListKeyCatalogReportAsync(null, CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.KeyNumber == "CAA-MST");
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.MasterAccessDisplay,
            KeyOpenedRoomDisplayFormatter.FormatAccess(reportMaster.Classification, reportMaster.OpenedRooms));

        IReadOnlyList<KeyLookupResult> byRoom = await lookup
            .SearchKeysAsync("101", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(byRoom, item => item.KeyNumber == "CAA-REG");
        Assert.Contains(byRoom, item => item.KeyNumber == "CAA-MST");

        Assert.Null(db.Model.FindEntityType(
            "KeyInventory.Infrastructure.Data.KeyAccessPatternRoomAssignmentEntity"));
        Assert.DoesNotContain(
            db.Model.GetEntityTypes().Select(entity => entity.GetTableName()),
            name => string.Equals(name, "KeyAccessPatternRoomAssignments", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExistingKeyNumberRejectsRoomAndClassificationChanges()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();

        await createDept.ExecuteAsync("caa2-dept", CancellationToken.None).ConfigureAwait(true);
        string roomCode = await createRoom.ExecuteAsync("caa2-dept", "201", "Office", CancellationToken.None)
            .ConfigureAwait(true);
        string otherRoom = await createRoom.ExecuteAsync("caa2-dept", "202", "Lab", CancellationToken.None)
            .ConfigureAwait(true);

        await createKey.RegisterNewKeyAsync(
                "CAA-EXIST",
                "01",
                KeyAccessClassification.Regular,
                [roomCode],
                CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                createKey.RegisterNewKeyAsync(
                    "CAA-EXIST",
                    "02",
                    KeyAccessClassification.Master,
                    null,
                    CancellationToken.None))
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                createKey.RegisterNewKeyAsync(
                    "CAA-EXIST",
                    "02",
                    null,
                    [otherRoom],
                    CancellationToken.None))
            .ConfigureAwait(true);

        RegisterNewKeyResult second = await createKey.RegisterNewKeyAsync(
                "CAA-EXIST",
                "02",
                null,
                null,
                CancellationToken.None)
            .ConfigureAwait(true);
        Assert.False(second.CreatedNewKeyNumber);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
