using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Domain.Parties;
using KeyInventory.Domain.Workforce;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class OperatorUxAtomicWorkforceRegistrationTests : IAsyncLifetime
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
    public async Task RegisterCreatesPartyAndWorkforceMemberAtomicallyWithGeneratedCodes()
    {
        using IServiceScope scope = CreateScope();
        await SeedDepartmentAsync(scope.ServiceProvider, "reg1").ConfigureAwait(true);

        string firstCode = await scope.ServiceProvider
            .GetRequiredService<IRegisterWorkforceMemberUseCase>()
            .ExecuteAsync(
                "Ada",
                "Lovelace",
                UniqueUin("reg1", 1),
                "Employee",
                "reg1-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<WorkforceMemberListItem> members = await scope.ServiceProvider
            .GetRequiredService<IListWorkforceMembersUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(members);
        WorkforceMemberListItem first = members[0];
        Assert.Equal(firstCode, first.WorkforceMemberCode);
        Assert.StartsWith("WM-", first.WorkforceMemberCode, StringComparison.Ordinal);
        Assert.StartsWith("PARTY-", first.PartyCode, StringComparison.Ordinal);
        Assert.Equal("Active", first.Status);

        string secondCode = await scope.ServiceProvider
            .GetRequiredService<IRegisterWorkforceMemberUseCase>()
            .ExecuteAsync(
                "Grace",
                "Hopper",
                UniqueUin("reg1", 3),
                "Contractor",
                "reg1-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.StartsWith("WM-", secondCode, StringComparison.Ordinal);
        WorkforceMemberListItem registered = (await scope.ServiceProvider
                .GetRequiredService<IListWorkforceMembersUseCase>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == secondCode);
        Assert.Equal("Grace", registered.FirstName);
        Assert.Equal("Hopper", registered.LastName);
        Assert.StartsWith("PARTY-", registered.PartyCode, StringComparison.Ordinal);
        Assert.Equal("Contractor", registered.WorkforceType);
    }

    [Fact]
    public async Task AddPartyAndWorkforceMemberRollsBackPartyWhenMemberPersistFails()
    {
        using IServiceScope scope = CreateScope();
        await SeedDepartmentAsync(scope.ServiceProvider, "rb1").ConfigureAwait(true);

        string existingCode = await scope.ServiceProvider
            .GetRequiredService<IRegisterWorkforceMemberUseCase>()
            .ExecuteAsync(
                "Ada",
                "Lovelace",
                UniqueUin("rb1", 1),
                "Employee",
                "rb1-dept",
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<WorkforceMemberListItem> existingMembers = await scope.ServiceProvider
                .GetRequiredService<IListWorkforceMembersUseCase>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(true);
        Assert.NotEmpty(existingMembers);
        WorkforceMemberListItem existing = existingMembers.Single(item => item.WorkforceMemberCode == existingCode);

        IWorkforcePersistencePort port = scope.ServiceProvider.GetRequiredService<IWorkforcePersistencePort>();
        Department? department = await port.FindDepartmentByCodeAsync("rb1-dept", CancellationToken.None)
            .ConfigureAwait(true);
        Assert.NotNull(department);
        string orphanPartyCode = $"PARTY-{Guid.NewGuid():D}";
        Party party = new(orphanPartyCode, "Orphan", "Party", UniqueUin("rb1", 9));
        WorkforceMember conflicting = new(
            existing.WorkforceMemberCode,
            orphanPartyCode,
            WorkforceType.Employee,
            department.DepartmentId);

        await Assert.ThrowsAnyAsync<Exception>(() =>
                port.AddPartyAndWorkforceMemberAsync(party, conflicting, CancellationToken.None))
            .ConfigureAwait(true);

        KeyInventoryDbContext db = scope.ServiceProvider.GetRequiredService<KeyInventoryDbContext>();
        Assert.False(await db.Parties.AnyAsync(entity => entity.PartyCode == orphanPartyCode).ConfigureAwait(true));
        Assert.Equal(1, await db.Parties.CountAsync().ConfigureAwait(true));
        Assert.Equal(1, await db.WorkforceMembers.CountAsync().ConfigureAwait(true));
    }

    [Fact]
    public async Task TerminationFromAuthorityRemainsFinalAndIssuedKeysPathWorks()
    {
        using IServiceScope scope = CreateScope();
        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "term")
            .ConfigureAwait(true);

        await scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>()
            .ExecuteAsync("TERM-KEY-1", "01", "mechanical", CancellationToken.None)
            .ConfigureAwait(true);

        DateTimeOffset issued = DateTimeOffset.UtcNow;
        DateTimeOffset due = issued.AddDays(1);
        await scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>()
            .ExecuteAsync(
                "loan-term-1",
                "TERM-KEY-1",
                "01",
                seeded.MemberCode,
                "Department",
                seeded.DepartmentCode,
                issued,
                due,
                CancellationToken.None)
            .ConfigureAwait(true);

        IReadOnlyList<IssuedKeyForMemberItem> before = await scope.ServiceProvider
            .GetRequiredService<IOperationalKeyLookupUseCase>()
            .ListIssuedKeysForWorkforceMemberAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(before);

        await scope.ServiceProvider.GetRequiredService<ITerminateWorkforceMemberUseCase>()
            .ExecuteAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);

        WorkforceMemberListItem terminated = (await scope.ServiceProvider
                .GetRequiredService<IListWorkforceMembersUseCase>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal("Terminated", terminated.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scope.ServiceProvider.GetRequiredService<IUpdateWorkforceMemberWorkforceTypeUseCase>()
                    .ExecuteAsync(seeded.MemberCode, "Contractor", CancellationToken.None))
            .ConfigureAwait(true);

        IReadOnlyList<IssuedKeyForMemberItem> after = await scope.ServiceProvider
            .GetRequiredService<IOperationalKeyLookupUseCase>()
            .ListIssuedKeysForWorkforceMemberAsync(seeded.MemberCode, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Single(after);
    }

    private static async Task SeedDepartmentAsync(IServiceProvider services, string prefix)
    {
        await services.GetRequiredService<ICreateDepartmentUseCase>()
            .ExecuteAsync($"{prefix}-dept", CancellationToken.None)
            .ConfigureAwait(true);
    }

    private static string UniqueUin(string prefix, int salt)
    {
        int hash = Math.Abs(HashCode.Combine(prefix, salt, "atomic")) % 1_000_000_000;
        return hash.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
