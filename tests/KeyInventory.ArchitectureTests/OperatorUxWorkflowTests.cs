using KeyInventory.Application.Lookup;
using KeyInventory.Application.Workforce;
using KeyInventory.Application.Workflow;
using KeyInventory.Infrastructure;
using KeyInventory.Infrastructure.Data;
using KeyInventory.Web.Presentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KeyInventory.ArchitectureTests;

public sealed class OperatorUxWorkflowTests : IAsyncLifetime
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
    public async Task IssueUsesEligibleDepartmentAndRoomChoicesAndLocalUtcBoundary()
    {
        using IServiceScope scope = CreateScope();
        ICreateKeyAssetUseCase createKey = scope.ServiceProvider.GetRequiredService<ICreateKeyAssetUseCase>();
        IIssueLoanUseCase issue = scope.ServiceProvider.GetRequiredService<IIssueLoanUseCase>();
        IListWorkforceMembersUseCase listMembers = scope.ServiceProvider.GetRequiredService<IListWorkforceMembersUseCase>();
        IListWorkAssignmentsUseCase listAssignments = scope.ServiceProvider.GetRequiredService<IListWorkAssignmentsUseCase>();
        IListRoomsUseCase listRooms = scope.ServiceProvider.GetRequiredService<IListRoomsUseCase>();
        IOperationalKeyLookupUseCase lookup = scope.ServiceProvider.GetRequiredService<IOperationalKeyLookupUseCase>();

        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "oux")
            .ConfigureAwait(true);
        await createKey.ExecuteAsync("OUX-KEY-1", "mechanical", CancellationToken.None).ConfigureAwait(true);

        WorkforceMemberListItem member = (await listMembers.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal("Ada", member.FirstName);
        Assert.Contains("UIN", PartyHolderDisplayFormatter.Format(member.FirstName, member.LastName, member.Uin), StringComparison.Ordinal);

        WorkforceMemberIdentityDisplay identity = (await lookup
                .ListActiveWorkforceMembersWithIdentityAsync(CancellationToken.None)
                .ConfigureAwait(true))
            .Single(item => item.WorkforceMemberCode == seeded.MemberCode);
        Assert.Equal(
            PartyHolderDisplayFormatter.Format(identity.FirstName, identity.LastName, identity.Uin),
            PartyHolderDisplayFormatter.Format(member.FirstName, member.LastName, member.Uin));

        string departmentCode = member.DepartmentCode;
        WorkAssignmentListItem assignment = (await listAssignments.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.IsActive && item.WorkforceMemberCode == seeded.MemberCode);
        RoomListItem room = (await listRooms.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => item.RoomCode == assignment.RoomCode);

        string localIssued = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow);
        string localDue = OperatorLocalTimestamp.ToControlValue(DateTimeOffset.UtcNow.AddDays(1));
        Assert.True(OperatorLocalTimestamp.TryParseToUtc(localIssued, out DateTimeOffset issuedUtc, out _), "issued");
        Assert.True(OperatorLocalTimestamp.TryParseToUtc(localDue, out DateTimeOffset dueUtc, out _), "due");

        await issue.ExecuteAsync(
                "loan-oux-dept",
                "OUX-KEY-1",
                seeded.MemberCode,
                "Department",
                departmentCode,
                issuedUtc,
                dueUtc,
                CancellationToken.None)
            .ConfigureAwait(true);

        await createKey.ExecuteAsync("OUX-KEY-2", "mechanical", CancellationToken.None).ConfigureAwait(true);
        await issue.ExecuteAsync(
                "loan-oux-room",
                "OUX-KEY-2",
                seeded.MemberCode,
                "Room",
                room.RoomCode,
                issuedUtc,
                dueUtc,
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal(2, (await scope.ServiceProvider.GetRequiredService<IListOpenLoansUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true)).Count);
    }

    [Fact]
    public async Task WorkforceListExposesPartyIdentityForOperatorSelectors()
    {
        using IServiceScope scope = CreateScope();
        IListPartiesUseCase listParties = scope.ServiceProvider.GetRequiredService<IListPartiesUseCase>();
        var seeded = await WorkforceEligibilityTestFixture.SeedEligibleMemberAsync(scope.ServiceProvider, "oux2")
            .ConfigureAwait(true);

        PartyListItem party = (await listParties.ExecuteAsync(CancellationToken.None).ConfigureAwait(true))
            .Single(item => string.Equals(item.PartyCode, seeded.PartyCode, StringComparison.Ordinal));
        Assert.Equal("Ada", party.FirstName);
        Assert.Equal(9, party.Uin.Length);

        IReadOnlyList<WorkforceMemberListItem> members = await scope.ServiceProvider
            .GetRequiredService<IListWorkforceMembersUseCase>()
            .ExecuteAsync(CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Contains(
            members,
            item => item.WorkforceMemberCode == seeded.MemberCode
                && item.FirstName == "Ada"
                && item.LastName == "Lovelace");
    }

    private IServiceScope CreateScope()
    {
        Assert.NotNull(_services);
        return _services.CreateScope();
    }
}
