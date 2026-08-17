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

/// <summary>
/// Behavioral authority tests for the single Application-owned New Key operation.
/// </summary>
public sealed class RegisterNewKeyWorkflowTests : IAsyncLifetime
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
    public async Task ExistingKeyNumberCreatesOnlyNewKeyAssetWithoutChangingAuthority()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase register = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        ICreateDepartmentUseCase createDepartment = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IListKeyAssetsUseCase listKeys = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();
        IGetKeyNumberRegistrationPreviewUseCase preview =
            scope.ServiceProvider.GetRequiredService<IGetKeyNumberRegistrationPreviewUseCase>();

        await createDepartment.ExecuteAsync("FAC", CancellationToken.None).ConfigureAwait(true);
        string roomCode = await CatalogSeedHelper
            .CreateRoomByDepartmentCodeAsync(scope.ServiceProvider, "FAC", "410D")
            .ConfigureAwait(true);

        RegisterNewKeyResult first = await register
            .RegisterNewKeyAsync("NK-100", "01", KeyAccessClassification.Master, [], CancellationToken.None)
            .ConfigureAwait(true);
        Assert.True(first.CreatedNewKeyNumber);

        RegisterNewKeyResult second = await register
            .RegisterNewKeyAsync("NK-100", "02", classification: null, roomCodes: null, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.False(second.CreatedNewKeyNumber);

        KeyNumberRegistrationPreview? after = await preview.ExecuteAsync("NK-100", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotNull(after);
        Assert.Equal(KeyAccessClassification.Master, after!.Classification);
        Assert.Empty(after.OpenedRooms);
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.MasterAccessDisplay,
            KeyOpenedRoomDisplayFormatter.FormatAccess(after.Classification, after.OpenedRooms));

        IReadOnlyList<KeyAssetListItem> keys = await listKeys.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(2, keys.Count(item => item.KeyNumber == "NK-100"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                register.RegisterNewKeyAsync(
                    "NK-100",
                    "03",
                    KeyAccessClassification.Regular,
                    [roomCode],
                    CancellationToken.None))
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task NonExistingRegularKeyNumberRequiresExactlyOneRoom()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase register = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        ICreateDepartmentUseCase createDepartment = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IGetKeyNumberRegistrationPreviewUseCase preview =
            scope.ServiceProvider.GetRequiredService<IGetKeyNumberRegistrationPreviewUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDepartment.ExecuteAsync("OPS", CancellationToken.None).ConfigureAwait(true);
        string roomA = await CatalogSeedHelper
            .CreateRoomByDepartmentCodeAsync(scope.ServiceProvider, "OPS", "101")
            .ConfigureAwait(true);
        string roomB = await CatalogSeedHelper
            .CreateRoomByDepartmentCodeAsync(scope.ServiceProvider, "OPS", "102")
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                register.RegisterNewKeyAsync(
                    "NK-200",
                    "10",
                    KeyAccessClassification.Regular,
                    [roomA, roomB],
                    CancellationToken.None))
            .ConfigureAwait(true);

        RegisterNewKeyResult result = await register
            .RegisterNewKeyAsync(
                "NK-200",
                "10",
                KeyAccessClassification.Regular,
                [roomA],
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.True(result.CreatedNewKeyNumber);
        KeyNumberRegistrationPreview? created = await preview.ExecuteAsync("NK-200", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotNull(created);
        Assert.Equal(KeyAccessClassification.Regular, created!.Classification);
        Assert.Single(created.OpenedRooms);
        Assert.Equal(roomA, created.OpenedRooms[0].RoomCode);
        Assert.Equal(1, await db.KeyAssets.CountAsync(item => item.KeyNumber == "NK-200").ConfigureAwait(true));
    }

    [Fact]
    public async Task NonExistingMasterKeyNumberForbidsRooms()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase register = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        ICreateDepartmentUseCase createDepartment = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IGetKeyNumberRegistrationPreviewUseCase preview =
            scope.ServiceProvider.GetRequiredService<IGetKeyNumberRegistrationPreviewUseCase>();

        await createDepartment.ExecuteAsync("MST", CancellationToken.None).ConfigureAwait(true);
        string roomA = await CatalogSeedHelper
            .CreateRoomByDepartmentCodeAsync(scope.ServiceProvider, "MST", "201")
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                register.RegisterNewKeyAsync(
                    "NK-MST",
                    "01",
                    KeyAccessClassification.Master,
                    [roomA],
                    CancellationToken.None))
            .ConfigureAwait(true);

        RegisterNewKeyResult result = await register
            .RegisterNewKeyAsync("NK-MST", "01", KeyAccessClassification.Master, [], CancellationToken.None)
            .ConfigureAwait(true);
        Assert.True(result.CreatedNewKeyNumber);

        KeyNumberRegistrationPreview? created = await preview.ExecuteAsync("NK-MST", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotNull(created);
        Assert.Equal(KeyAccessClassification.Master, created!.Classification);
        Assert.Empty(created.OpenedRooms);
    }

    [Fact]
    public async Task NewKeyNumberWithoutMedecoOrClassificationIsRejectedAndLeavesNoOrphan()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase register = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
                register.RegisterNewKeyAsync("NK-300", " ", KeyAccessClassification.Regular, null, CancellationToken.None))
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                register.RegisterNewKeyAsync("NK-301", "01", classification: null, roomCodes: null, CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Equal(0, await db.KeyAccessPatterns.CountAsync(item => item.KeyNumber.StartsWith("NK-30")).ConfigureAwait(true));
        Assert.Equal(0, await db.KeyAssets.CountAsync(item => item.KeyNumber.StartsWith("NK-30")).ConfigureAwait(true));
    }

    [Fact]
    public async Task InvalidRoomOnNewKeyNumberLeavesNoOrphanKeyNumber()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase register = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                register.RegisterNewKeyAsync(
                    "NK-400",
                    "01",
                    KeyAccessClassification.Regular,
                    ["missing-room"],
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.False(await db.KeyAccessPatterns.AnyAsync(item => item.KeyNumber == "NK-400").ConfigureAwait(true));
        Assert.False(await db.KeyAssets.AnyAsync(item => item.KeyNumber == "NK-400").ConfigureAwait(true));
    }

    [Fact]
    public void RegisterPresentationAndDepartmentTerminologyContracts()
    {
        string register = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/KeyInventory.Web/Pages/Catalog/Register.cshtml"));
        string registerCode = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/KeyInventory.Web/Pages/Catalog/Register.cshtml.cs"));
        string deptAdd = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/KeyInventory.Web/Pages/Administration/Departments/Add.cshtml"));
        string deptEdit = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/KeyInventory.Web/Pages/Administration/Departments/Edit.cshtml"));
        string layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/KeyInventory.Web/Pages/Shared/_Layout.cshtml"));

        Assert.Contains("New Key", register, StringComparison.Ordinal);
        Assert.Contains("Replace Lost Key", register, StringComparison.Ordinal);
        Assert.Contains("data-allow-custom-value=\"true\"", register, StringComparison.Ordinal);
        Assert.Contains("Create Key", register, StringComparison.Ordinal);
        Assert.Contains("RegisterNewKeyAsync", registerCode, StringComparison.Ordinal);
        Assert.Contains("new-key-room-block", register, StringComparison.Ordinal);
        Assert.Contains("master-access-hint", register, StringComparison.Ordinal);
        Assert.DoesNotContain("register-selected-rooms", register, StringComparison.Ordinal);
        Assert.Contains("Room Assignments", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Work Assignments", layout, StringComparison.Ordinal);

        string webRoot = Path.Combine(FindRepoRoot(), "src", "KeyInventory.Web", "Pages");
        foreach (string file in Directory.EnumerateFiles(webRoot, "*.cs*", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            Assert.DoesNotContain("KeyInventoryDbContext", content, StringComparison.Ordinal);
        }

        Assert.Contains(
            "<label>\n            Department\n",
            deptAdd.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
        Assert.Contains(
            "<label>\n            Department\n",
            deptEdit.Replace("\r\n", "\n", StringComparison.Ordinal),
            StringComparison.Ordinal);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services!.CreateScope();
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "KeyInventory.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
