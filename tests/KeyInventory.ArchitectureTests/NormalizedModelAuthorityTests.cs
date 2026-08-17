using KeyInventory.Application.Catalog;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Catalog;
using KeyInventory.Domain.Locations;
using KeyInventory.Domain.Loans;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

/// <summary>
/// Authority tests for the normalized Department/Room/KEY #/Loan model after KeyType removal.
/// </summary>
public sealed class NormalizedModelAuthorityTests : IAsyncLifetime
{
    private static readonly Guid DepartmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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
    public void RoomRequiresDepartmentId()
    {
        Assert.Throws<ArgumentException>(() => new Room("r1", "101", Guid.Empty, "Lab"));
        Room room = new("r1", "101", DepartmentId, "Lab");
        Assert.Equal(DepartmentId, room.DepartmentId);
    }

    [Fact]
    public void KeyNumberClassificationIsExplicitNotInferredFromRoomCount()
    {
        KeyAccessPattern regular = new("KEY-R", KeyAccessClassification.Regular, "room-a");
        Assert.Equal(KeyAccessClassification.Regular, regular.Classification);
        Assert.Equal("room-a", regular.RoomCode);
        Assert.False(regular.OpensAllRooms);

        KeyAccessPattern master = new("KEY-M", KeyAccessClassification.Master, null);
        Assert.Empty(master.OpenedRoomCodes);
        Assert.Null(master.RoomCode);
        Assert.True(master.OpensAllRooms);
        Assert.Equal(KeyAccessClassification.Master, master.Classification);
    }

    [Fact]
    public void KeyAssetCanExistWithNoLoan()
    {
        KeyAsset key = CatalogTestFactory.CreateCopy("KEY-NL", "01", KeyAccessClassification.Regular);
        Assert.Equal("KEY-NL", key.KeyNumber);
        Assert.Equal("01", key.MedecoKeyCode);
        Assert.Equal(KeyPhysicalCondition.Active, key.Condition);
        Assert.True(key.IsIssuableCondition);
    }

    [Fact]
    public void DomainAllowsMultipleUnassignedCopiesUnderSameKeyNumber()
    {
        KeyAccessPattern pattern = CatalogTestFactory.CreatePattern("KEY-MULTI", KeyAccessClassification.Regular);
        KeyAsset copyA = new(Guid.NewGuid(), pattern, "26");
        KeyAsset copyB = new(Guid.NewGuid(), pattern, "27");

        Assert.Equal(pattern.KeyNumber, copyA.KeyNumber);
        Assert.Equal(pattern.KeyNumber, copyB.KeyNumber);
        Assert.NotEqual(copyA.MedecoKeyCode, copyB.MedecoKeyCode);
        Assert.Equal(pattern.RoomCode, copyA.AccessPattern.RoomCode);
        Assert.Equal(pattern.RoomCode, copyB.AccessPattern.RoomCode);
    }

    [Fact]
    public void DomainEnforcesAtMostOneOpenLoanPerKeyAssetViaReturnGate()
    {
        KeyAsset key = CatalogTestFactory.CreateCopy("KEY-LOAN", "01");
        DateTimeOffset issued = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        Loan open = new(
            "loan-1",
            key,
            "party-1",
            issued,
            issued.AddDays(1),
            KeyIssueJustificationKind.Department,
            DepartmentId,
            "DEPT",
            null);

        Assert.True(open.IsOpenForReturn);
        _ = new Return("return-1", open, issued.AddHours(1));
        Assert.False(open.IsOpenForReturn);
        Assert.Throws<InvalidOperationException>(() => new Return("return-2", open, issued.AddHours(2)));
    }

    [Fact]
    public async Task WorkAssignmentRejectsCrossDepartmentRoom()
    {
        using IServiceScope scope = CreateScope();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IRegisterWorkforceMemberUseCase register = scope.ServiceProvider.GetRequiredService<IRegisterWorkforceMemberUseCase>();
        ICreateWorkAssignmentUseCase createWa = scope.ServiceProvider.GetRequiredService<ICreateWorkAssignmentUseCase>();

        await createDept.ExecuteAsync("norm-dept-a", CancellationToken.None).ConfigureAwait(true);
        await createDept.ExecuteAsync("norm-dept-b", CancellationToken.None).ConfigureAwait(true);
        string roomB = await createRoom.ExecuteAsync("norm-dept-b", "201", "Other dept room", CancellationToken.None)
            .ConfigureAwait(true);
        string memberA = await register.ExecuteAsync(
                "Norm",
                "Worker",
                "111222333",
                nameof(WorkforceType.Employee),
                "norm-dept-a",
                CancellationToken.None)
            .ConfigureAwait(true);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                createWa.ExecuteAsync(memberA, roomB, CancellationToken.None))
            .ConfigureAwait(true);
        Assert.Contains("Cross-department", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitRegularAndMasterArePersistedIndependentlyOfRooms()
    {
        using IServiceScope scope = CreateScope();
        IListKeyAssetsUseCase list = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "NORM-REG", "01", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "NORM-MST", "01", KeyAccessClassification.Master, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyAssetListItem> keys = await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(
            KeyAccessClassification.Regular,
            keys.Single(item => item.KeyNumber == "NORM-REG").Classification);
        Assert.Equal(
            KeyAccessClassification.Master,
            keys.Single(item => item.KeyNumber == "NORM-MST").Classification);
        Assert.Single(keys.Single(item => item.KeyNumber == "NORM-REG").OpenedRooms);
        Assert.Empty(keys.Single(item => item.KeyNumber == "NORM-MST").OpenedRooms);
        Assert.Equal(
            KeyOpenedRoomDisplayFormatter.MasterAccessDisplay,
            KeyOpenedRoomDisplayFormatter.FormatAccess(
                KeyAccessClassification.Master,
                keys.Single(item => item.KeyNumber == "NORM-MST").OpenedRooms));
    }

    [Fact]
    public async Task MultipleCopiesUnderKeyNumberRemainUnassignedUntilIssued()
    {
        using IServiceScope scope = CreateScope();
        IListKeyAssetsUseCase list = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();
        IListOpenLoansUseCase openLoans = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "NORM-COPIES", "26", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "NORM-COPIES", "27", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyAssetListItem> copies = (await list.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Where(item => item.KeyNumber == "NORM-COPIES")
            .ToArray();
        Assert.Equal(2, copies.Count);
        Assert.Empty(await openLoans.ExecuteAsync(CancellationToken.None).ConfigureAwait(true));
    }

    [Fact]
    public async Task AtMostOneOpenLoanPerKeyAssetAndReceivePreservesAssetForReissue()
    {
        using IServiceScope scope = CreateScope();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IListKeyAssetsUseCase listKeys = scope.ServiceProvider.GetRequiredService<IListKeyAssetsUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "norm-loan")
            .ConfigureAwait(true);
        await CatalogSeedHelper.CreatePhysicalKeyAsync(
                scope.ServiceProvider, "NORM-ISSUE", "01", KeyAccessClassification.Regular, CancellationToken.None)
            .ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 14, 15, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-norm-1",
                "NORM-ISSUE",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                issue.ExecuteAsync(
                    "loan-norm-2",
                    "NORM-ISSUE",
                    "01",
                    seeded.MemberCode,
                    "Department",
                    seeded.DepartmentCode,
                    issued.AddMinutes(1),
                    issued.AddDays(1),
                    CancellationToken.None))
            .ConfigureAwait(true);

        await completeReturn.ExecuteAsync("return-norm-1", "loan-norm-1", issued.AddHours(2), CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal(1, await db.KeyAssets.CountAsync(item => item.KeyNumber == "NORM-ISSUE" && item.MedecoKeyCode == "01")
            .ConfigureAwait(true));
        Assert.Contains(
            await listKeys.ExecuteAsync(CancellationToken.None).ConfigureAwait(true),
            item => item.KeyNumber == "NORM-ISSUE" && item.MedecoKeyCode == "01"
                && item.Condition == KeyPhysicalCondition.Active);

        await issue.ExecuteAsync(
                "loan-norm-3",
                "NORM-ISSUE",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued.AddHours(3),
                issued.AddDays(2),
                CancellationToken.None)
            .ConfigureAwait(true);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
