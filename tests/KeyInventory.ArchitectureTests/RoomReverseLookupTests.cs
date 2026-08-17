using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class RoomReverseLookupTests : IAsyncLifetime
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

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }

    [Fact]
    public async Task RoomSearchReturnsRegularMatchPlusAllMasters()
    {
        using IServiceScope scope = CreateScope();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "rr-room")
            .ConfigureAwait(true);
        string room410 = await createRoom.ExecuteAsync(seeded.DepartmentCode, "410D", "Office", CancellationToken.None)
            .ConfigureAwait(true);
        string room411 = await createRoom.ExecuteAsync(seeded.DepartmentCode, "411A", "Lab", CancellationToken.None)
            .ConfigureAwait(true);

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, room410, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "27", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "28", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "MASTER1", "01", KeyAccessClassification.Master, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "UNRELATED", "99", KeyAccessClassification.Regular, room411, CancellationToken.None)
            .ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 12, 15, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-rr-27",
                "66800",
                "27",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> by410 = await lookup.SearchKeysAsync("410D", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(by410, item => item.KeyNumber == "66800");
        Assert.Contains(by410, item => item.KeyNumber == "MASTER1");
        Assert.DoesNotContain(by410, item => item.KeyNumber == "UNRELATED");

        Assert.Equal(3, by410.Count(item => item.KeyNumber == "66800"));
        Assert.Contains(
            by410,
            item => item.KeyNumber == "66800"
                && item.MedecoKeyCode == "27"
                && item.AvailabilityStatus == OperationalKeyAvailability.Issued
                && item.CurrentHolder is not null);

        KeyLookupResult issuedCopy = by410.Single(item => item.KeyNumber == "66800" && item.MedecoKeyCode == "27");
        Assert.Contains(issuedCopy.OpenedRooms, room => room.RoomNumber == "410D");
        Assert.Single(issuedCopy.OpenedRooms);

        KeyLookupResult master = by410.Single(item => item.KeyNumber == "MASTER1");
        Assert.Empty(master.OpenedRooms);
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.MasterAccessDisplay,
            KeyOpenedRoomDisplayFormatter.FormatAccess(master.Classification, master.OpenedRooms));

        IReadOnlyList<KeyLookupResult> by411 = await lookup.SearchKeysAsync("411A", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(by411, item => item.KeyNumber == "MASTER1");
        Assert.Contains(by411, item => item.KeyNumber == "UNRELATED");
        Assert.DoesNotContain(by411, item => item.KeyNumber == "66800");
    }

    [Fact]
    public async Task RoomNumberRenameKeepsRelationshipAndUpdatesSearchAuthority()
    {
        using IServiceScope scope = CreateScope();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IUpdateRoomNumberUseCase updateRoom = scope.ServiceProvider.GetRequiredService<IUpdateRoomNumberUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();

        await scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>()
            .ExecuteAsync("rr-rename-dept", CancellationToken.None).ConfigureAwait(true);
        string roomCode = await createRoom.ExecuteAsync("rr-rename-dept", "410D", "Office", CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, roomCode)
            .ConfigureAwait(true);

        Assert.Contains(
            await lookup.SearchKeysAsync("410D", CancellationToken.None).ConfigureAwait(true),
            item => item.KeyNumber == "66800");

        await updateRoom.ExecuteAsync(roomCode, "HALL-9", CancellationToken.None).ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> byNew = await lookup.SearchKeysAsync("HALL-9", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(byNew, item => item.KeyNumber == "66800");
        Assert.Contains(
            byNew.Single(item => item.KeyNumber == "66800").OpenedRooms,
            room => room.RoomNumber == "HALL-9" && room.RoomCode == roomCode);

        IReadOnlyList<KeyLookupResult> byOld = await lookup.SearchKeysAsync("410D", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.DoesNotContain(byOld, item => item.KeyNumber == "66800");
    }

    [Fact]
    public async Task ExistingKeyNumberMedecoAndClassificationSearchRemainIntact()
    {
        using IServiceScope scope = CreateScope();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();

        await scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>()
            .ExecuteAsync("rr-search-dept", CancellationToken.None).ConfigureAwait(true);
        string roomCode = await createRoom.ExecuteAsync("rr-search-dept", "410D", "Office", CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "66800", "26", KeyAccessClassification.Regular, roomCode, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "MASTER1", "01", KeyAccessClassification.Master, CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Contains(
            await lookup.SearchKeysAsync("66800", CancellationToken.None).ConfigureAwait(true),
            item => item.KeyNumber == "66800" && item.MedecoKeyCode == "26");
        Assert.Contains(
            await lookup.SearchKeysAsync("26", CancellationToken.None).ConfigureAwait(true),
            item => item.KeyNumber == "66800" && item.MedecoKeyCode == "26");
        Assert.Contains(
            await lookup.SearchKeysAsync("master", CancellationToken.None).ConfigureAwait(true),
            item => item.KeyNumber == "MASTER1");
        Assert.DoesNotContain(
            await lookup.SearchKeysAsync("master", CancellationToken.None).ConfigureAwait(true),
            item => item.KeyNumber == "66800");
    }

    [Fact]
    public void LookupAdapterOwnsRoomTraversalNotWeb()
    {
        string adapter = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Infrastructure/Lookup/OperationalKeyLookupAdapter.cs"));
        Assert.Contains("RoomNumber.Contains", adapter, StringComparison.Ordinal);
        Assert.Contains("KeyAccessClassification.Master", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyAccessPatternRoomAssignments", adapter, StringComparison.Ordinal);

        string findPage = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src/KeyInventory.Web/Pages/Operations/Find.cshtml.cs"));
        Assert.Contains("IOperationalKeyLookupUseCase", findPage, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyInventoryDbContext", findPage, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyAccessPatternRoomAssignment", findPage, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "KeyInventory.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
