using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class KeyLookupWorkflowTests : IAsyncLifetime
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
    public async Task SearchSupportsExactAndPartialKeyCodeWithAvailableAndIssuedHolders()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "lk-search")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("LK-MASTER-1", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("LK-MASTER-2", "electronic", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("OTHER-9", "mechanical", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-lk-1",
                "LK-MASTER-1",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<KeyLookupResult> exact = await lookup.SearchKeysAsync("LK-MASTER-1", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(exact);
        Assert.Equal(OperationalKeyAvailability.Issued, exact[0].AvailabilityStatus);
        PartyHolderDisplay holder = Assert.IsType<PartyHolderDisplay>(exact[0].CurrentHolder);
        Assert.Equal("Ada", holder.FirstName);
        Assert.Equal("Lovelace", holder.LastName);
        Assert.False(string.IsNullOrWhiteSpace(holder.Uin));
        Assert.Equal(9, holder.Uin.Length);
        Assert.Equal("loan-lk-1", exact[0].OpenLoanCode);

        IReadOnlyList<KeyLookupResult> partial = await lookup.SearchKeysAsync("LK-MASTER", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal(2, partial.Count);
        Assert.Contains(partial, item =>
            item.CatalogKeyCode == "LK-MASTER-1" && item.AvailabilityStatus == OperationalKeyAvailability.Issued);
        Assert.Contains(partial, item =>
            item.CatalogKeyCode == "LK-MASTER-2"
            && item.AvailabilityStatus == OperationalKeyAvailability.Available
            && item.CurrentHolder is null);

        IReadOnlyList<KeyLookupResult> byType = await lookup.SearchKeysAsync("electronic", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(byType, item => item.CatalogKeyCode == "LK-MASTER-2");
    }

    [Fact]
    public async Task MemberIssuedKeysResolveThroughPartyAndOpenLoans()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "lk-member")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("key-lk-m1", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await createKey.ExecuteAsync("key-lk-m2", "mechanical", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 8, 13, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-lk-m1",
                "key-lk-m1",
                seeded.MemberCode,
                "Room",
                seeded.RoomCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);
        await issue.ExecuteAsync(
                "loan-lk-m2",
                "key-lk-m2",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<IssuedKeyForMemberItem> keys = await lookup
            .ListIssuedKeysForWorkforceMemberAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal(2, keys.Count);
        Assert.All(keys, item =>
        {
            Assert.Equal("Ada", item.HolderFirstName);
            Assert.Equal("Lovelace", item.HolderLastName);
            Assert.False(string.IsNullOrWhiteSpace(item.HolderUin));
        });
        Assert.Contains(keys, item => item.CatalogKeyCode == "key-lk-m1");
        Assert.Contains(keys, item => item.CatalogKeyCode == "key-lk-m2");
    }

    [Fact]
    public async Task OpenAndReturnedLoanDisplaysResolveHolderIdentityAndIssueReceiveRemainValid()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        ICompleteReturnUseCase completeReturn = scope.ServiceProvider.GetRequiredService<ICompleteReturnUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();
        IListOpenLoansUseCase listOpen = scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "lk-flow")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("key-lk-flow", "mechanical", CancellationToken.None).ConfigureAwait(true);

        DateTimeOffset issued = new(2026, 8, 8, 14, 0, 0, TimeSpan.Zero);
        await issue.ExecuteAsync(
                "loan-lk-flow",
                "key-lk-flow",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                issued.AddDays(1),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<OperationalLoanDisplay> open = await lookup.ListOpenLoansWithHoldersAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(open, loan =>
            loan.LoanCode == "loan-lk-flow"
            && loan.HolderFirstName == "Ada"
            && loan.HolderLastName == "Lovelace"
            && loan.HolderUin.Length == 9);

        IReadOnlyList<LoanListItem> openRaw = await listOpen.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(openRaw, loan =>
            loan.LoanCode == "loan-lk-flow" && loan.BorrowerPartyReference == seeded.PartyCode);

        await completeReturn.ExecuteAsync(
                "return-lk-flow",
                "loan-lk-flow",
                issued.AddHours(2),
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<OperationalLoanDisplay> returned =
            await lookup.ListReturnedLoansWithHoldersAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(returned, loan =>
            loan.LoanCode == "loan-lk-flow"
            && loan.HolderFirstName == "Ada"
            && loan.ReturnedAtUtc == issued.AddHours(2));
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
