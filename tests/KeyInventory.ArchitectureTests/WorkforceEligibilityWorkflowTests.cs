using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class WorkforceEligibilityWorkflowTests : IAsyncLifetime
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
    public async Task PersistenceRoundTripsWorkforceEntitiesAndRoomNumberUniqueness()
    {
        using IServiceScope scope = CreateScope();
        ICreateOrganizationUseCase createOrg = scope.ServiceProvider.GetRequiredService<ICreateOrganizationUseCase>();
        ICreateDepartmentUseCase createDept = scope.ServiceProvider.GetRequiredService<ICreateDepartmentUseCase>();
        ICreateBuildingUseCase createBuilding = scope.ServiceProvider.GetRequiredService<ICreateBuildingUseCase>();
        ICreateRoomUseCase createRoom = scope.ServiceProvider.GetRequiredService<ICreateRoomUseCase>();
        IListRoomsUseCase listRooms = scope.ServiceProvider.GetRequiredService<IListRoomsUseCase>();

        await createOrg.ExecuteAsync("org-p", CancellationToken.None).ConfigureAwait(true);
        await createDept.ExecuteAsync("org-p", "dept-p", CancellationToken.None).ConfigureAwait(true);
        await createBuilding.ExecuteAsync("bldg-p", CancellationToken.None).ConfigureAwait(true);
        await createRoom.ExecuteAsync("room-p1", "bldg-p", "101", "One", CancellationToken.None).ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            createRoom.ExecuteAsync("room-p2", "bldg-p", "101", "Dup", CancellationToken.None));

        IReadOnlyList<RoomListItem> rooms = await listRooms.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(rooms, room => room.RoomCode == "room-p1" && room.RoomNumber == "101");
    }

    [Fact]
    public async Task IssueKeyRequiresEligibilityAndUsesPartyBorrower()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "wf-issue")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("key-wf-1", "mechanical", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-wf-1",
                "key-wf-1",
                seeded.MemberCode,
                "Room",
                seeded.RoomCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<LoanListItem> open = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(open, loan => loan.LoanCode == "loan-wf-1" && loan.BorrowerPartyReference == seeded.PartyCode);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            issue.ExecuteAsync(
                "loan-wf-2",
                "key-wf-1",
                seeded.MemberCode,
                "Department",
                "wrong-dept",
                issued,
                issued.AddDays(1),
                CancellationToken.None));
    }

    [Fact]
    public async Task TerminationBlocksIssuesAndExposesObligationsWithoutMutatingLoans()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ITerminateWorkforceMemberUseCase terminate = scope.ServiceProvider.GetRequiredService<ITerminateWorkforceMemberUseCase>();
        IListOutstandingReturnObligationsUseCase obligations =
            scope.ServiceProvider.GetRequiredService<IListOutstandingReturnObligationsUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();
        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "wf-term")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("key-wf-term", "mechanical", CancellationToken.None).ConfigureAwait(true);
        DateTimeOffset issued = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-wf-term",
                "key-wf-term",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        string loanStatusBefore = await db.Loans.AsNoTracking()
            .Where(loan => loan.LoanCode == "loan-wf-term")
            .Select(loan => loan.Status)
            .SingleAsync()
            .ConfigureAwait(true);

        await terminate.ExecuteAsync(seeded.MemberCode, CancellationToken.None).ConfigureAwait(true);
        IReadOnlyList<OutstandingReturnObligationItem> outstanding =
            await obligations.ExecuteAsync(seeded.MemberCode, CancellationToken.None).ConfigureAwait(true);

        Assert.Contains(outstanding, item => item.LoanCode == "loan-wf-term");

        string loanStatusAfter = await db.Loans.AsNoTracking()
            .Where(loan => loan.LoanCode == "loan-wf-term")
            .Select(loan => loan.Status)
            .SingleAsync()
            .ConfigureAwait(true);
        Assert.Equal(loanStatusBefore, loanStatusAfter);
        Assert.Equal(1, await db.Loans.CountAsync().ConfigureAwait(true));
        Assert.Equal(0, await db.Returns.CountAsync().ConfigureAwait(true));

        IReadOnlyList<LoanListItem> open = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(open, loan => loan.LoanCode == "loan-wf-term");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            issue.ExecuteAsync(
                "loan-wf-blocked",
                "key-wf-term",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(2),
                CancellationToken.None));
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
