using KeyInventory.Application.Catalog;
using KeyInventory.Application.Lifecycle;
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

public sealed class ConfigurationLifecycleWorkflowTests : IAsyncLifetime
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
    public async Task UnreferencedDepartmentCanBeDeletedAndReferencedMustRetire()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();
        IRegisterWorkforceMemberUseCase register =
            scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("tmp-dept", CancellationToken.None).ConfigureAwait(true);
        DepartmentLifecycleItem unused = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "tmp-dept");
        Assert.True(unused.Capabilities.CanEdit);
        Assert.True(unused.Capabilities.CanDelete);
        Assert.False(unused.Capabilities.CanRetire);

        await lifecycle.DeleteDepartmentAsync(unused.DepartmentId, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, await db.Departments.CountAsync(item => item.DepartmentCode == "tmp-dept")
            .ConfigureAwait(true));

        await createDept.ExecuteAsync("used-dept", CancellationToken.None).ConfigureAwait(true);
        await register.ExecuteAsync(
                "Ada",
                "Lovelace",
                "123456789",
                nameof(WorkforceType.Employee),
                "used-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        DepartmentLifecycleItem referenced = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "used-dept");
        Assert.True(referenced.Capabilities.CanEdit);
        Assert.False(referenced.Capabilities.CanDelete);
        Assert.True(referenced.Capabilities.CanRetire);

        InvalidOperationException blocked = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                lifecycle.DeleteDepartmentAsync(referenced.DepartmentId, CancellationToken.None))
            .ConfigureAwait(true);
        Assert.Contains("Retire", blocked.Message, StringComparison.OrdinalIgnoreCase);

        await lifecycle.RetireDepartmentAsync(referenced.DepartmentId, CancellationToken.None).ConfigureAwait(true);
        DepartmentLifecycleItem retired = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "used-dept");
        Assert.False(retired.IsActive);
        Assert.True(retired.Capabilities.CanActivate);
        Assert.False(retired.Capabilities.CanDelete);

        await lifecycle.ActivateDepartmentAsync(referenced.DepartmentId, CancellationToken.None).ConfigureAwait(true);
        Assert.True((await lifecycle.ListDepartmentsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "used-dept").IsActive);
    }

    [Fact]
    public async Task DepartmentDeleteRejectsWhenRelationshipAppearsAfterList()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();
        IRegisterWorkforceMemberUseCase register =
            scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();

        await createDept.ExecuteAsync("race-dept", CancellationToken.None).ConfigureAwait(true);
        DepartmentLifecycleItem raceDept = (await lifecycle.ListDepartmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "race-dept");
        Assert.True(raceDept.Capabilities.CanDelete);

        await register.ExecuteAsync(
                "Grace",
                "Hopper",
                "987654321",
                nameof(WorkforceType.Employee),
                "race-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                lifecycle.DeleteDepartmentAsync(raceDept.DepartmentId, CancellationToken.None))
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task UnreferencedRoomMedecoAndKeyNumberLifecycle()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("lc-dept", CancellationToken.None).ConfigureAwait(true);
        string roomCode = await createRoom.ExecuteAsync("lc-dept", "999", "Temp", CancellationToken.None)
            .ConfigureAwait(true);
        RoomLifecycleItem room = (await lifecycle.ListRoomsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == roomCode);
        Assert.True(room.Capabilities.CanEdit);
        Assert.True(room.Capabilities.CanDelete);
        await lifecycle.DeleteRoomAsync(roomCode, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, await db.Rooms.CountAsync(item => item.RoomCode == roomCode).ConfigureAwait(true));

        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "LC-KEY", "01", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);

        KeyAssetLifecycleItem medeco = (await lifecycle.ListKeyAssetsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.KeyNumber == "LC-KEY" && item.MedecoKeyCode == "01");
        Assert.True(medeco.Capabilities.CanDelete);
        Assert.False(medeco.Capabilities.CanEdit);
        await lifecycle.DeleteKeyAssetAsync(medeco.KeyAssetId, CancellationToken.None).ConfigureAwait(true);

        KeyAccessPatternLifecycleItem pattern = (await lifecycle.ListKeyAccessPatternsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.KeyNumber == "LC-KEY");
        Assert.True(pattern.Capabilities.CanDelete);
        await lifecycle.DeleteKeyAccessPatternAsync("LC-KEY", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, await db.KeyAccessPatterns.CountAsync(item => item.KeyNumber == "LC-KEY").ConfigureAwait(true));

        string room2 = await createRoom.ExecuteAsync("lc-dept", "998", "Lab", CancellationToken.None).ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "LC-KEY2", "02", KeyAccessClassification.Regular, room2, CancellationToken.None)
            .ConfigureAwait(true);
        // KEY # with a physical copy cannot be deleted; Regular KEY # blocks Room delete.
        Assert.False((await lifecycle.ListKeyAccessPatternsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.KeyNumber == "LC-KEY2").Capabilities.CanDelete);
        Assert.False((await lifecycle.ListRoomsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == room2).Capabilities.CanDelete);
        await lifecycle.RetireRoomAsync(room2, CancellationToken.None).ConfigureAwait(true);
        Assert.True((await lifecycle.ListRoomsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == room2).Capabilities.CanActivate);
    }


    [Fact]
    public async Task UnusedWorkforceMemberAndActiveWorkAssignmentCanDeleteUsedCannot()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IRegisterWorkforceMemberUseCase register =
            scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        ICreateWorkAssignmentUseCase createWa =
            scope.ServiceProvider.GetRequiredService<ICreateWorkAssignmentUseCase>();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IConfigurationLifecycleUseCase lifecycle =
            scope.ServiceProvider.GetRequiredService<IConfigurationLifecycleUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        await createDept.ExecuteAsync("wm-dept", CancellationToken.None).ConfigureAwait(true);
        string unusedMember = await register.ExecuteAsync(
                "Temp",
                "User",
                "111222333",
                nameof(WorkforceType.Employee),
                "wm-dept",
                CancellationToken.None)
            .ConfigureAwait(true);
        Assert.True((await lifecycle.ListWorkforceMembersAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == unusedMember).Capabilities.CanDelete);
        await lifecycle.DeleteWorkforceMemberAsync(unusedMember, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(
            0,
            await db.WorkforceMembers.CountAsync(item => item.WorkforceMemberCode == unusedMember)
                .ConfigureAwait(true));

        string roomCode = await createRoom.ExecuteAsync("wm-dept", "777", "Office", CancellationToken.None)
            .ConfigureAwait(true);
        string memberCode = await register.ExecuteAsync(
                "Used",
                "Person",
                "444555666",
                nameof(WorkforceType.Employee),
                "wm-dept",
                CancellationToken.None)
            .ConfigureAwait(true);
        await createWa.ExecuteAsync(memberCode, roomCode, CancellationToken.None)
            .ConfigureAwait(true);
        WorkAssignmentLifecycleItem activeWa = (await lifecycle.ListWorkAssignmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.IsActive
                && item.WorkforceMemberCode == memberCode
                && item.RoomCode == roomCode);
        Assert.True(activeWa.Capabilities.CanDelete);
        Assert.False((await lifecycle.ListWorkforceMembersAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == memberCode).Capabilities.CanDelete);

        await lifecycle.DeleteWorkAssignmentAsync(activeWa.WorkAssignmentId, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(
            0,
            await db.WorkAssignments.CountAsync(item => item.WorkAssignmentId == activeWa.WorkAssignmentId)
                .ConfigureAwait(true));

        await createWa.ExecuteAsync(memberCode, roomCode, CancellationToken.None)
            .ConfigureAwait(true);
        WorkAssignmentLifecycleItem secondWa = (await lifecycle.ListWorkAssignmentsAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.IsActive
                && item.WorkforceMemberCode == memberCode
                && item.RoomCode == roomCode);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(scope.ServiceProvider, "WM-KEY", "09", KeyAccessClassification.Regular, CancellationToken.None).ConfigureAwait(true);
        DateTimeOffset issued = DateTimeOffset.UtcNow;
        await issue.ExecuteAsync(
                "loan-lc-1",
                "WM-KEY",
                "09",
                memberCode,
                "Department",
                "wm-dept",
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False((await lifecycle.ListWorkAssignmentsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkAssignmentId == secondWa.WorkAssignmentId).Capabilities.CanDelete);
        Assert.False((await lifecycle.ListKeyAssetsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.KeyNumber == "WM-KEY" && item.MedecoKeyCode == "09").Capabilities.CanDelete);
        Assert.False((await lifecycle.ListWorkforceMembersAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == memberCode).Capabilities.CanDelete);
        Assert.False((await lifecycle.ListDepartmentsAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.DepartmentCode == "wm-dept").Capabilities.CanDelete);
    }

    [Fact]
    public void WebLifecyclePagesExposeEditOnEditableRowsAndAvoidDbContext()
    {
        string rooms = Read("src/KeyInventory.Web/Pages/Administration/Rooms/Index.cshtml");
        Assert.Contains("Capabilities.CanEdit", rooms, StringComparison.Ordinal);
        Assert.Contains("Edit", rooms, StringComparison.Ordinal);

        string departments = Read("src/KeyInventory.Web/Pages/Administration/Departments/Index.cshtml");
        Assert.Contains("Capabilities.CanEdit", departments, StringComparison.Ordinal);
        Assert.Contains(">Edit<", departments, StringComparison.Ordinal);
        Assert.Contains("Capabilities.CanDelete", departments, StringComparison.Ordinal);
        Assert.Contains("Capabilities.CanRetire", departments, StringComparison.Ordinal);

        string members = Read("src/KeyInventory.Web/Pages/Administration/WorkforceMembers/Index.cshtml");
        Assert.Contains("Capabilities.CanEdit", members, StringComparison.Ordinal);
        Assert.Contains(">Edit<", members, StringComparison.Ordinal);

        string keys = Read("src/KeyInventory.Web/Pages/Catalog/Keys.cshtml");
        Assert.Contains("DeletePattern", keys, StringComparison.Ordinal);
        Assert.Contains("Capabilities.CanRetire", keys, StringComparison.Ordinal);

        string[] pageCodes =
        [
            "src/KeyInventory.Web/Pages/Administration/Departments/Index.cshtml.cs",
            "src/KeyInventory.Web/Pages/Administration/Departments/Delete.cshtml.cs",
            "src/KeyInventory.Web/Pages/Administration/Departments/Edit.cshtml.cs",
            "src/KeyInventory.Web/Pages/Administration/Rooms/Index.cshtml.cs",
            "src/KeyInventory.Web/Pages/Catalog/Keys.cshtml.cs"
        ];
        foreach (string relative in pageCodes)
        {
            string code = Read(relative);
            Assert.DoesNotContain("DbContext", code, StringComparison.Ordinal);
            if (relative.EndsWith("Departments/Edit.cshtml.cs", StringComparison.Ordinal))
            {
                Assert.Contains("IUpdateDepartmentCodeUseCase", code, StringComparison.Ordinal);
                Assert.Contains("IConfigurationLifecycleUseCase", code, StringComparison.Ordinal);
                continue;
            }

            Assert.Contains("IConfigurationLifecycleUseCase", code, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BusinessForeignKeysRemainRestrict()
    {
        string[] configs =
        [
            "src/KeyInventory.Infrastructure/Data/WorkforceMemberConfiguration.cs",
            "src/KeyInventory.Infrastructure/Data/KeyAssetConfiguration.cs",
            "src/KeyInventory.Infrastructure/Data/LoanConfiguration.cs",
            "src/KeyInventory.Infrastructure/Data/KeyAccessPatternConfiguration.cs",
            "src/KeyInventory.Infrastructure/Data/ReturnConfiguration.cs"
        ];

        foreach (string relative in configs)
        {
            string text = Read(relative);
            Assert.Contains("DeleteBehavior.Restrict", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DeleteBehavior.Cascade", text, StringComparison.Ordinal);
        }
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }

    private static string Read(string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(RepoRoot(), relativePath));
        return File.ReadAllText(path);
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
